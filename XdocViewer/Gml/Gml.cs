using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sharp.Gml
{
    #region Gml-DOM

    /// <summary>
    /// represents a node in a generic markup language DOM. this is the simplest element in the document.
    /// </summary>
    public class GmlNode
    {
        #region Constructors

        /// <summary>
        /// protected, parameterless constructor.
        /// </summary>
        public GmlNode()
        {
            Attributes = new List<TagAttribute>();
            Children = new List<GmlNode>();
        }

        /// <summary>
        /// default constructor: just specify the tag.
        /// </summary>
        /// <param name="tagName"></param>
        public GmlNode(string tagName)
            : this()
        {
            this.Name = tagName;
        }

        /// <summary>
        /// construct; specify tag and attributes.
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="attributes"></param>
        public GmlNode(string tagName, params TagAttribute[] attributes)
            : this()
        {
            this.Name = tagName;
            this.Attributes.AddRange(attributes);
        }

        /// <summary>
        /// construct, specifying a query for the contents.
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="contents"></param>
        public GmlNode(string tagName, IEnumerable<GmlNode> contents)
            : this()
        {
            this.Name = tagName;
            foreach (var child in contents)
            {
                child.SetParent(this);
            }
        }

        /// <summary>
        /// create a node as a child of the specified parent. adds itself to the parent's children collection.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="tagName"></param>
        /// <param name="attributes"></param>
        /// <param name="contents"></param>
        public GmlNode(GmlNode parent, string tagName, IEnumerable<TagAttribute> attributes = null, IEnumerable<GmlNode> contents = null)
            : this()
        {
            this.Name = tagName;
            this.Parent = parent;
            foreach (var child in contents)
            {
                child.SetParent(this);
            }
            this.Attributes.AddRange(attributes);
        }

        /// <summary>
        /// create a node as a child of the specified node.
        /// adds itself to the parent's children collection.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="attributes"></param>
        public GmlNode(GmlNode parent, string name, params TagAttribute[] attributes)
            : this()
        {
            this.Name = name;
            this.Parent = parent;
            this.Attributes.AddRange(attributes);
        }

        #endregion Constructors

        /// <summary>
        /// adds an existing node to this node's children.
        /// </summary>
        /// <param name="node"></param>
        public GmlNode Add(GmlNode node)
        {
            // specify me as the parent of the node:
            node.SetParent(this);

            // return the node reference:
            return node;
        }

        #region Markup Properties

        /// <summary>
        /// if true, appends the node to the target document in-line;
        /// </summary>
        public bool Inline { get; set; }

        /// <summary>
        /// list of element attributes for this node.
        /// </summary>
        public List<TagAttribute> Attributes { get; protected set; }

        /// <summary>
        /// the name of the node/the tag-name.
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// returns the path to the node in the current heirarchy
        /// </summary>
        public string Path
        {
            get
            {
                if (Parent != null)
                    return Parent.Path + "\\" + Name;
                else
                    return Name;
            }
        }

        /// <summary>
        /// gets the  markup for the children of this node.
        /// </summary>
        public virtual string Markup
        {
            get
            {
                return RenderMarkup(new GmlStringFormatter()).Markup;
            }
        }

        public bool TextOnlyChild
        {
            get
            {
                return (this.Children.Count == 1 && this.Children[0] is GmlText);
            }
        }

        /// <summary>
        /// gets the inner-markup (the contents of the element, not including the tag)
        /// </summary>
        public string InnerMarkup
        {
            get { return RenderInnerMarkup(new GmlStringFormatter()).Markup; }
        }

        #endregion Markup Properties

        #region Hierarchy Properties

        /// <summary>
        /// the parent node.
        /// </summary>
        public GmlNode Parent { get; protected set; }

        /// <summary>
        /// child nodes in the hierarchy
        /// </summary>
        public IList<GmlNode> Children { get; protected set; }

        /// <summary>
        /// all ancestors, starting from this.Parent
        /// </summary>
        public IEnumerable<GmlNode> Ancestors
        {
            get
            {
                var node = this.Parent;
                while (node != null)
                {
                    yield return node;
                    node = node.Parent;
                }
            }
        }

        /// <summary>
        /// all child and descended nodes.
        /// </summary>
        public IEnumerable<GmlNode> Descendants
        {
            get
            {
                Stack<GmlNode> nodes = new Stack<GmlNode>();
                nodes.Push(this);
                while (nodes.Count > 0)
                {
                    var node = nodes.Pop();
                    yield return node;
                    foreach (var child in node.Children)
                    {
                        nodes.Push(child);
                    }
                }
            }
        }

        #endregion Hierarchy Properties

        /// <summary>
        /// sets the parent of this node, adds this node into the new parent.
        /// </summary>
        /// <param name="parentNode"></param>
        public void SetParent(GmlNode parentNode)
        {
            if (this.Parent != null)
            {
                // remove this node from the children of the current parent:
                this.Parent.Children.Remove(this);
            }

            // assign the new parent node;
            this.Parent = parentNode;

            // add this node into children of the new parent:
            if (!this.Parent.Children.Contains(this))
                this.Parent.Children.Add(this);
        }

        /// <summary>
        /// generates the inner-markup;
        /// </summary>
        /// <param name="hb"></param>
        /// <returns></returns>
        public virtual GmlFormatter RenderInnerMarkup(GmlFormatter hb)
        {
            if (this.Children.Count > 0)
            {
                foreach (var child in Children)
                    child.RenderMarkup(hb);
            }
            return hb;
        }

        /// <summary>
        /// uses a <see cref="GmlFormatter"/> to render markup for the current node and it's children.
        /// </summary>
        /// <param name="fmt"></param>
        /// <returns></returns>
        public virtual GmlFormatter RenderMarkup(GmlFormatter fmt)
        {
            if (Children.Count == 0)
            {
                // close in start-tag:
                // when there is no text value nor children, close immediately (eg <a href="http://www.google.com"/> or <br/>)
                fmt.OpenInline(this.Name, true, this.Attributes.ToArray()).NewLine();
            }
            else
            {
                // the inline flag allows the element to be written inline <p>like this</p>
                // rather than nested
                // <p>
                //      <div>
                //          like this
                //      </div>
                // </p>
                if (this.Inline || this.Name.Equals("pre", StringComparison.OrdinalIgnoreCase))
                {
                    // inline tag: (formatting option, don't start on a new line)
                    fmt.OpenInline(this.Name, this.Attributes.ToArray());

                    // add the rest of the element:
                    RenderInnerMarkup(fmt);

                    // close on the same line:
                    fmt.CloseInline();
                }
                else if (this.TextOnlyChild)
                {
                    fmt.OpenInline(this.Name, this.Attributes.ToArray());
                    this.Children[0].RenderMarkup(fmt);
                    fmt.CloseInline().NewLine();
                }
                else
                {
                    // multi-line tag:
                    fmt.Open(this.Name, this.Attributes.ToArray());
                    RenderInnerMarkup(fmt);
                    fmt.Close();
                }
            }
            return fmt;
        }

        /// <summary>
        /// returns a string display of the node.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // get the tag markup:
            return String.Format("<{0}{1}>",
                this.Name, TagAttribute.ToMarkupString(this.Attributes));
        }
    }

    /// <summary>
    /// represents a text node in a markup language DOM.
    /// </summary>
    public class GmlText : GmlNode
    {
        #region Constructor

        protected GmlText()
            : base()
        {
            this.Name = "#text";
        }

        public GmlText(GmlNode parent, string value)
            : this()
        {
            this.Parent = parent;
            this.Value = value;
        }

        #endregion Constructor

        /// <summary>
        /// gets or sets the text value of the node.
        /// </summary>
        public string Value { get; set; }

        public override string ToString()
        {
            return this.Value;
        }

        /// <summary>
        /// adds the text to the markup.
        /// </summary>
        /// <param name="fmt"></param>
        /// <returns></returns>
        public override GmlFormatter RenderMarkup(GmlFormatter fmt)
        {
            return fmt.AppendText(this.Value, true);
        }
    }

    /// <summary>
    /// represents a comment node in a markup-language Document Object Model.
    /// </summary>
    public class GmlComment : GmlNode
    {
        #region Constructor

        protected GmlComment()
            : base()
        {
            this.Name = "#comment";
        }

        public GmlComment(GmlNode parent, string comment)
            : this()
        {
            // set the comment:
            this.Comment = comment;

            // add into the parent node:
            this.Parent = parent;
            this.Parent.Children.Add(this);
        }

        #endregion Constructor

        /// <summary>
        /// gets or sets the comment for the comment node.
        /// </summary>
        public string Comment { get; set; }

        public override string ToString()
        {
            return "// " + this.Comment;
        }

        /// <summary>
        /// override the markup:
        /// </summary>
        /// <param name="hb"></param>
        /// <returns></returns>
        public override GmlFormatter RenderMarkup(GmlFormatter hb)
        {
            if (!string.IsNullOrWhiteSpace(this.Comment))
                return hb.Comment(this.Comment);
            else
                return hb;
        }
    }

    /// <summary>
    /// represents an element in a markup language document.
    /// </summary>
    public class GmlElement : GmlNode
    {
        #region Constructors

        protected GmlElement()
            : base()
        {
            this.AutoAddNodes = true;
        }

        /// <summary>
        /// construct with a name
        /// </summary>
        /// <param name="name"></param>
        public GmlElement(string name)
            : this()
        {
            this.Name = name;
        }

        /// <summary>
        /// construct with a parent, name and attributes.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="attribs"></param>
        public GmlElement(GmlNode parent, string name, params TagAttribute[] attribs)
            : base(parent, name, attribs)
        {
            if (parent is GmlElement)
                this.AutoAddNodes = ((GmlElement)parent).AutoAddNodes;
            else
                this.AutoAddNodes = true;
        }


        /// <summary>
        /// construct a new element as a child of the specified parent, with the given name, a text node and a list of attribute values.
        /// </summary>
        /// <param name="parent">the parent node/element</param>
        /// <param name="name">the tag-name for the element node</param>
        /// <param name="text">the value of the child text node</param>
        /// <param name="attributes">list of attribute names and values</param>
        public GmlElement(GmlNode parent, string name, string text, params TagAttribute[] attributes)
            : this(parent, name, attributes)
        {
            if (parent is GmlElement)
                this.AutoAddNodes = ((GmlElement)parent).AutoAddNodes;
            else
                this.AutoAddNodes = true;

            if (!string.IsNullOrEmpty(text))
                this.Children.Add(new GmlText(this, text));
        }

        /// <summary>
        /// construct the element-node;
        /// </summary>
        /// <param name="name"></param>
        /// <param name="attribs"></param>
        public GmlElement(string name, params TagAttribute[] attribs)
            : base(name, attribs)
        {
            this.AutoAddNodes = true;
        }

        #endregion Constructors

        #region Default Attribute Property:

        /// <summary>
        /// gets or sets attributes by index value.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public TagAttribute this[int idx]
        {
            get
            {
                if (Attributes.Count > idx)
                    return Attributes[idx];
                else
                    return null;
            }

            set
            {
                if (Attributes.Count > idx)
                    Attributes[idx] = value;
                else
                    throw new ApplicationException("index out of range!");
            }
        }

        /// <summary>
        /// gets an element-attribute by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TagAttribute this[string name]
        {
            get
            {
                return GetAttributeByName(name);
            }

            set
            {
                // is there an attribute with this name?
                var attribute = GetAttributeByName(name);
                if (attribute != null)
                {
                    // find it's index in the list:
                    var idx = Attributes.IndexOf(attribute);
                    if (idx >= 0)
                    {
                        // replace it:
                        Attributes[idx] = value;
                        return;
                    }
                }

                // no existing attribute with this name: so just add to the end of the list.
                Attributes.Add(value);
            }
        }

        /// <summary>
        /// fetches attribute by name;
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private TagAttribute GetAttributeByName(string name)
        {
            // get an element-attribute by name:
            var attrib = (from a in Attributes where a.Name.Equals(name) select a).FirstOrDefault();
            if (attrib == null)
            {
                return (from a in Attributes where a.Name.Equals(name, StringComparison.OrdinalIgnoreCase) select a).FirstOrDefault();
            }
            else
                return attrib;
        }

        #endregion Default Attribute Property:

        #region Common Attribute Properties

        /// <summary>
        /// set the value of the specified attribute within the attributes collection. if the attribute does not already exist it is added.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void SetAttribute(string name, string value)
        {
            var attribute = GetAttributeByName(name);
            if (attribute != null)
                attribute.Value = value;
            else
            {
                Attributes.Add(new TagAttribute(name, value));
            }
        }

        /// <summary>
        /// gets the value of the named attribute.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetAttribute(string name)
        {
            var attribute = GetAttributeByName(name);
            if (attribute != null)
                return attribute.Value;
            else
                return null;
        }

        /// <summary>
        /// gets or sets the ID element-attribute value for this node.
        /// </summary>
        public string IdAttribute
        {
            get { return GetAttribute("id"); }
            set { SetAttribute("id", value); }
        }

        /// <summary>
        /// gets or sets the css class element-attribute for this node.
        /// </summary>
        public string ClassAttribute
        {
            get { return GetAttribute("class"); }
            set { SetAttribute("class", value); }
        }

        /// <summary>
        /// gets or sets the style attribute (inline css) for this node.
        /// </summary>
        public string StyleAttribute
        {
            get { return GetAttribute("style"); }
            set { SetAttribute("style", value); }
        }

        /// <summary>
        /// gets or sets the href atttribute  for this node,.
        /// </summary>
        public string HrefAttribute
        {
            get { return GetAttribute("href"); }
            set { SetAttribute("href", value); }
        }

        /// <summary>
        /// gets or sets the on-click attribute for this node.
        /// </summary>
        public string OnClickAttribute
        {
            get { return GetAttribute("onclick"); }
            set { SetAttribute("onclick", value); }
        }

        #endregion Common Attribute Properties

        #region Add Methods

        /// <summary>
        /// adds a new child-node to this node's children with the given name and attributes.
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public GmlElement Add(string tagName, params TagAttribute[] attributes)
        {
            return AddElement(new GmlElement(this, tagName, attributes));
        }

        /// <summary>
        /// adds a new child node with the given tag, attributes and contents.
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="attributes"></param>
        /// <param name="contents"></param>
        /// <returns></returns>
        public GmlElement Add(string tagName, IEnumerable<TagAttribute> attributes, IEnumerable<GmlNode> contents)
        {
            var node = AddElement(new GmlElement(this, tagName, attributes.ToArray()));
            foreach (var n in contents)
            {
                if (n.Parent != null)
                    n.Parent.Children.Remove(node);

                node.Children.Add(node);
            }
            return node;
        }

        /// <summary>
        /// adds an element to this element's child collection.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public GmlElement AddElement(GmlElement element)
        {
            element.Parent = this;
            element.AutoAddNodes = this.AutoAddNodes;
            Children.Add(element);
            return element;
        }

        /// <summary>
        /// creates a comment node and adds it to the parent.
        /// </summary>
        /// <param name="comment"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public GmlComment AddComment(string comment)
        {
            var cm = new GmlComment(this, comment);
            this.Add(cm);
            return cm;
        }

        /// <summary>
        /// adds a text node as a child of this node.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public GmlText AddText(string text)
        {
            var t = new GmlText(this, text);
            this.Children.Add(t);
            return t;
        }

        /// <summary>
        /// when true, builder methods automatically add to the parent.
        /// </summary>
        public bool AutoAddNodes { get; set; }


        /// <summary>
        /// shortcut method to create a new element for the current node;
        /// if AutoAddNodes is true, then the new element is automaticall added to the child collection.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement CreateElement(string name, params TagAttribute[] att)
        {
            // create a new element with the name and attributes
            var element = new GmlElement(name, att);

            // either return the element or add it to this nodes children then return it;
            return this.AutoAddNodes ? this.AddElement(element) : element;
        }

        /// <summary>
        /// shortcut method to create a new element for the current node and apply text.
        /// if AutoAddNodes is true, then the new element is automatically added to the child collection.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement CreateElement(string name, string text, params TagAttribute[] att)
        {
            var node = this.AutoAddNodes ? AddElement(new GmlElement(this, name, att)) : new GmlElement(this, name, att);

            if (!string.IsNullOrWhiteSpace(text))
                node.AddText(text);

            return node;
        }

        #endregion Add Methods

        #region HTML Node Methods

        /// <summary>
        /// generates a division node.
        /// </summary>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement Div(params TagAttribute[] att)
        {
            return CreateElement("div", att);
        }

        /// <summary>
        /// generates a division node with the given id, class and attributes.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cls"></param>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement Div(string id, string cls, params TagAttribute[] att)
        {
            return CreateElement("div", new[] { TagAttribute.GetId(id), TagAttribute.GetClass(cls) }.Union(att).ToArray());
        }

        /// <summary>
        /// generates a division node with the given class.
        /// </summary>
        /// <param name="cls"></param>
        /// <returns></returns>
        public GmlElement Div(string cls, params TagAttribute[] att)
        {
            return CreateElement("div", new[] { TagAttribute.GetClass(cls) }.Union(att).ToArray());
        }


        public GmlElement Heading(int heading)
        {
            var node = CreateElement("h" + heading);
            node.Inline = true;
            return node;
        }

        public GmlElement Heading(int headingNo, string headingText = null, params TagAttribute[] attributes)
        {
            var node = CreateElement("h" + headingNo);
            node.Inline = true;
            if (!string.IsNullOrEmpty(headingText))
            {
                node.AddText(headingText);
            }
            return node;
        }

        public GmlElement I(params TagAttribute[] att)
        {
            return CreateElement("i", att);
        }

        public GmlElement I(string text, params TagAttribute[] att)
        {
            return CreateElement("i", text, att);
        }

        /// <summary>
        /// create a button node.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="attribs"></param>
        /// <returns></returns>
        public GmlElement Button(ButtonType type, string name, string value, params TagAttribute[] attribs)
        {
            var node = CreateElement("button", new[] { new TagAttribute("name", name), new TagAttribute("type", type.ToString()) }.Union(attribs).ToArray());
            if (!string.IsNullOrEmpty(value))
            {
                node.AddText(value);
            }
            return node;
        }

        /// <summary>
        /// creates a paragraph node, adds it to the current node, creates a text-node if paragraphText is supplied.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="paragraphText"></param>
        /// <param name="attribs"></param>
        /// <returns></returns>
        public GmlElement P(string paragraphText = null, params TagAttribute[] attribs)
        {
            var node = CreateElement("p", attribs);
            if (!string.IsNullOrEmpty(paragraphText))
            {
                node.AddText(paragraphText);
            }
            return node;
        }

        /// <summary>
        /// creates a "bold" node.
        /// </summary>
        /// <param name="boldText"></param>
        /// <param name="attribs"></param>
        /// <returns></returns>
        public GmlElement B(string boldText = null, params TagAttribute[] attribs)
        {
            var node = CreateElement("b", attribs);
            if (!string.IsNullOrEmpty(boldText))
            {
                node.AddText(boldText);
            }
            return node;
        }

        /// <summary>
        /// creates a "bold" node.
        /// </summary>
        /// <param name="boldText"></param>
        /// <param name="attribs"></param>
        /// <returns></returns>
        public GmlElement Strong(string boldText = null, params TagAttribute[] attribs)
        {
            var node = CreateElement("strong", attribs);
            if (!string.IsNullOrEmpty(boldText))
            {
                node.AddText(boldText);
            }
            return node;
        }

        /// <summary>
        /// creates a Pre (preserve) node.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="attribs"></param>
        /// <returns></returns>
        public GmlElement Pre(string text = null, params TagAttribute[] attribs)
        {
            var node = CreateElement("pre", attribs);
            if (!string.IsNullOrEmpty(text))
            {
                node.AddText(text);
            }
            return node;
        }

        /// <summary>
        /// creates a code node.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="attribs"></param>
        /// <returns></returns>
        public GmlElement Code(string text = null, params TagAttribute[] attribs)
        {
            var node = CreateElement("code", attribs);
            if (!string.IsNullOrEmpty(text))
            {
                node.AddText(text);
            }
            return node;
        }

        /// <summary>
        /// creates a new &lt;br&gt; node.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="attribs"></param>
        /// <returns></returns>
        public GmlElement Br()
        {
            var node = CreateElement("br");
            node.Inline = true;
            return node;
        }

        /// <summary>
        /// creates a new &lt;hr&gt; node.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="attribs"></param>
        /// <returns></returns>
        public GmlElement Hr()
        {
            var node = CreateElement("hr");
            node.Inline = true;
            return node;
        }

        /// <summary>
        /// creates a hyperlink (a) node
        /// </summary>
        /// <param name="href"></param>
        /// <param name="text"></param>
        /// <param name="attribs"></param>
        /// <returns></returns>
        public GmlElement Hyperlink(string href, string text, params TagAttribute[] attribs)
        {
            // create an "a" element node:
            var node = new GmlElement(this, "a", attribs) { Inline = true };

            // add in the href attribute:
            node.Attributes.Add(TagAttribute.GetHref(href));

            // add in the child text node:
            if (!string.IsNullOrEmpty(text))
            {
                node.AddText(text);
            }

            // return the new node:
            return AutoAddNodes ? AddElement(node) : node;
        }


        #region Defintiion List

        /// <summary>
        /// create a description list node.
        /// </summary>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement DL(params TagAttribute[] att)
        {
            return CreateElement("dl", att);
        }

        /// <summary>
        /// create a description-term node;
        /// </summary>
        /// <param name="item"></param>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement DT(params TagAttribute[] att)
        {
            return CreateElement("dt", att);
        }

        /// <summary>
        /// create a description-term node;
        /// </summary>
        /// <param name="item"></param>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement DT(string item, params TagAttribute[] att)
        {
            return CreateElement("dt", item, att);
        }

        /// <summary>
        /// create a description-definition node;
        /// </summary>
        /// <param name="description"></param>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement DD(string description, params TagAttribute[] att)
        {
            return CreateElement("dd", description, att);
        }

        /// <summary>
        /// create a description-definition node;
        /// </summary>
        /// <param name="description"></param>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement DD(params TagAttribute[] att)
        {
            return CreateElement("dd", att);
        }

        #endregion Defintiion List

        #region Definition Builder Helpers

        /// <summary>
        /// helper to build a definition description of the items in the list using table tags.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public GmlElement DefinitionTable(params GmlItem[] items)
        {
            // create the table holder:
            var table = new GmlTable(new TagAttribute("border", "0"));

            // add each item as a row:
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    var tr = table.AddRow(item.Attributes.ToArray());
                    tr.AddTD().B(item.Value + ":");
                    tr.AddTD().AddText(item.Description);
                }
            }

            // add the table node to the child collection & return
            if (AutoAddNodes)
            {
                return AddElement(table.TableNode);
            }
            else
            {
                // just return:
                return table.TableNode;
            }
        }

        /// <summary>
        /// helper to build a definition description of the items in the list as a DL type list.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public GmlElement DefinitionList(params GmlItem[] items)
        {
            // create the list:
            var dl = this.DL();

            // add each item:
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    // add the definition term:
                    dl.DT(item.Attributes.ToArray()).B(item.Value + ":");

                    // add the definition description:
                    dl.DD().P(item.Description);
                }
            }

            // return the top node;
            return dl;
        }

        #endregion Definition Builder Helpers

        #region Bullet List(s)

        /// <summary>
        /// un-ordered bullet list.
        /// </summary>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement UL(params TagAttribute[] att)
        {
            var node = new GmlElement(this, "ul", att);
            if (AutoAddNodes)
                return AddElement(node);
            else
                return node;
        }

        /// <summary>
        /// ordered bullet-list
        /// </summary>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement OL(params TagAttribute[] att)
        {
            var node = new GmlElement(this, "ol", att);
            if (AutoAddNodes)
                return AddElement(node);
            else
                return node;
        }

        /// <summary>
        /// list-item.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="att"></param>
        /// <returns></returns>
        public GmlElement Li(string value = null, params TagAttribute[] att)
        {
            var node = new GmlElement(this, "li", att);
            if (!string.IsNullOrEmpty(value))
                node.AddText(value);
            if (AutoAddNodes)
                return AddElement(node);
            else
                return node;
        }

        /// <summary>
        /// generates an ordered or un-orderd list-node, populated with the specified items.
        /// </summary>
        /// <param name="ordered"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public GmlElement List(bool ordered, GmlItem[] items)
        {
            GmlElement l = null;
            if (ordered)
                l = OL();
            else
                l = UL();

            foreach (var item in items)
            {
                // create each list-item:
                var li = l.Li(null, item.Attributes.ToArray());
                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    li.B(item.Value);
                    li.Br();
                    li.AddText(item.Description);
                }
                else
                {
                    li.AddText(item.Value);
                }
            }
            return l;
        }

        #endregion Bullet List(s)

        #endregion HTML Node Methods
    }

    #endregion Gml-DOM

    #region Parser

    /// <summary>
    /// gml parser state enumeration
    /// </summary>
    public enum GmlParserState
    {
        /// <summary>
        /// at the start of a document
        /// </summary>
        Init,
        /// <summary>
        /// currently reading a &lt;tag&gt;
        /// </summary>
        ReadingTag,
        /// <summary>
        /// currently reading an element-attribute name
        /// </summary>
        ReadingAttributeName,
        /// <summary>
        /// currently reading an element-attribute value
        /// </summary>
        ReadingAttributeValue,
        /// <summary>
        /// currently reading a &lt;!-- comment --&gt;
        /// </summary>
        ReadingComment,
        /// <summary>
        /// currently reading text
        /// </summary>
        ReadingText
    }

    /// <summary>
    /// assists with parsing generic markup language documents. loads a document from a stream or a byte array, and provides methods to move through the document characters
    /// and classify the current character type or peek at characters forward or backward of the current position (without losing the current position)
    /// </summary>
    public class GmlParserHelper
    {
        /// <summary>
        /// helper method - enumerates the stream byte by byte. effectively queries a file using linq.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static IEnumerable<byte> Enumerate(Stream s)
        {
            int b = s.ReadByte();
            while (b >= 0)
            {
                yield return (byte)b;
                b = s.ReadByte();
            }
        }

        public static string ReadStream(Stream s, out Encoding encoding)
        {
            using (var rdr = new StreamReader(s, true))
            {
                var str  = rdr.ReadToEnd();
                encoding = rdr.CurrentEncoding;
                return str;
            }
        }

        /// <summary>
        /// the document bytes
        /// </summary>
        //private byte[] bytes = null;

        /// <summary>
        /// the current position
        /// </summary>
        private int _currentPos = -1;

        /// <summary>
        /// construct from an array of bytes
        /// </summary>
        /// <param name="data"></param>
        public GmlParserHelper(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                Encoding encoding;
                this.StringData = ReadStream(ms, out encoding);
                this.StringEncoding = encoding;
                //bytes = Encoding.ASCII.GetBytes(this.StringData);

            }
        }

        public Encoding StringEncoding { get; protected set; }

        public string StringData
        {
            get; set;
        }

        /// <summary>
        /// construct from a stream
        /// </summary>
        /// <param name="data"></param>
        public GmlParserHelper(Stream data)
        {
            Encoding encoding;
            this.StringData     = ReadStream(data, out encoding);
            this.StringEncoding = encoding;
            //this.bytes          = Encoding.ASCII.GetBytes(this.StringData);
        }

        /// <summary>
        /// increments the current position by the specified amount, returns false if the increment would move past the end of the file.
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        public bool Increment(int by)
        {
            if (_currentPos + by < StringData.Length)
            {
                _currentPos += by;
                return true;
            }
            return false;
        }

        /// <summary>
        /// gets the byte at the current position
        /// </summary>
        public byte CurrentByte { get { return (byte)StringData[_currentPos]; } }

        /// <summary>
        /// gets the character at the current position
        /// </summary>
        public char CurrentChar { get { return StringData[_currentPos]; } }

        /// <summary>
        /// is the current character whitespace?
        /// </summary>
        public bool IsWhiteSpace { get { return char.IsWhiteSpace(CurrentChar); } }

        /// <summary>
        /// is the current character a tag-open character (&lt;)
        /// </summary>
        public bool IsTagOpen { get { return CurrentChar == '<'; } }

        /// <summary>
        /// is the current character a tag-close character (&gt;)
        /// </summary>
        public bool IsTagClose { get { return CurrentChar == '>'; } }

        /// <summary>
        /// is the current character a text delimiter?
        /// </summary>
        public bool IsTextDelim { get { return CurrentChar == '"'; } }

        /// <summary>
        /// is the current position at the end of a comment? (ie, is the current char the first dash of --&gt;)
        /// </summary>
        public bool EndComment
        {
            get
            {
                return (CurrentChar == '-' && NextChar == '-' && NextNextChar == '>');
            }
        }

        /// <summary>
        /// returns true if the current position is sitting on the '/' in a closing tag ( &lt;/&gt; )
        /// </summary>
        public bool IsTagComplete
        {
            get { return (CurrentChar == '/' && (NextChar == '>' || PrevChar == '<')); }
        }

        /// <summary>
        /// peeks at the next, non-whitespace character, without changing the current position
        /// </summary>
        public char PeekSkipWhiteSpace
        {
            get
            {
                int i = _currentPos;
                while (char.IsWhiteSpace(StringData[i]))
                    i++;

                return StringData[i];
            }
        }

        /// <summary>
        /// peeks forward into the document and determines if there is a closing tag for the specified opening tag.
        /// </summary>
        /// <param name="tag">the tag-name eg body</param>
        /// <returns>
        /// true if there is a closing tag for the specified opener - eg &lt;/body&gt;
        /// </returns>
        public bool HasClosingTag(string tag)
        {
            var test = new StringBuilder();
            var predicate = "</" + tag + ">";

            for (int i = _currentPos; i < StringData.Length; i++)
            {
                test.Append((char)StringData[i]);
                if (test.Length > predicate.Length * 2)
                    test.Remove(0, 1);
                if (test.ToString().Contains(predicate))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// peeks at the next character
        /// </summary>
        public char NextChar
        {
            get
            {
                if ((_currentPos + 1) < StringData.Length)
                    return StringData[_currentPos + 1];
                else
                    return (char)0;
            }
        }

        /// <summary>
        /// peeks at the character after the next.
        /// </summary>
        public char NextNextChar
        {
            get
            {
                if ((_currentPos + 2) < StringData.Length)
                    return StringData[_currentPos + 2];
                else
                    return StringData[StringData.Length - 1];
            }
        }

        /// <summary>
        /// peeks at the previous character
        /// </summary>
        public char PrevChar
        {
            get
            {
                if (_currentPos > 0)
                    return StringData[_currentPos - 1];
                else
                    return (char)0;
            }
        }

        /// <summary>
        /// moves the current position to the next character
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if ((_currentPos + 1) < StringData.Length)
            {
                _currentPos++;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// generic markup language parser. parses XML, HTML, XHTML, XAML etc. Tolerant of loose standards (eg HTML web-pages)
    /// </summary>
    public static class GmlParser
    {
        public static GmlDocument Parse(string markup)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(markup)))
            {
                return Parse(ms);
            }
        }

        public static GmlDocument Parse(Stream markup)
        {

            // the document holder:
            var doc = new GmlDocument();

            return Parse(markup, doc);
        }

        /// <summary>
        /// parse generic markup-language into a hierarchal DOM structure;
        /// text inside &lt; &gt; characters is considered a tag-name. any text inside the &lt; &gt; characters after a space
        /// is an attribute-name/value pair (  eg size="10")
        /// if there is no '=' then the name is considered a boolean set to true, and the text after the whitespace is the next attribute.
        /// &lt;/tag&gt; indicates a closing tag.
        /// tags ending with /&gt; indicate the tag is opened and closed with no children. if there is no corresponding &lt;/tag&gt; for any
        /// &lt;tag&gt;, it is also considered opened and closed with no children (eg &lt;br&gt;, &lt;!DOCTYPE&gt;)
        ///
        /// Reasonably tolerant of the loose HTML specifications.
        /// </summary>
        /// <param name="markup">the document stream</param>
        /// <returns>
        /// produces a <see cref="GmlDocument"/> populated with the markup parsed from the stream.
        /// </returns>
        public static GmlDocument Parse(Stream markup, GmlDocument doc)
        {
            // load the document bytes into a helper object, (the cursor)
            var cursor = new GmlParserHelper(markup);

            // the current element (defaut to the document element)
            GmlElement current = doc;

            // set the encoding;
            doc.Encoding = cursor.StringEncoding;

            // list of nodes as-generated:
            var nodes = new List<GmlNode>();

            // builders:
            var tagName     = new StringBuilder();
            var text        = new StringBuilder();
            var attribName  = new StringBuilder();
            var attribValue = new StringBuilder();
            var commentText = new StringBuilder();

            // flags:
            bool isCloseTag = false;
            bool inQuotes = false;

            // state machine holders for current and previous state
            var mode = GmlParserState.Init;
            var last = mode;

            // current element attributes
            var attributes = new List<TagAttribute>();

            // keep going while there are characters in the document:
            while (cursor.MoveNext())
            {
                // mode-context switch
                switch (mode)
                {
                    case GmlParserState.Init:
                        // initialize: expect a < character or error
                        if (cursor.IsTagOpen)
                            mode = GmlParserState.ReadingTag;
                        else
                            throw new ApplicationException("Unexpected character: '" + cursor.CurrentChar + "' expected: '<'");
                        break;

                    case GmlParserState.ReadingTag:
                        // reading a tag-value, break to attribute if whitespace:
                        if (cursor.IsWhiteSpace)
                        {
                            // expecting attribute name:
                            mode = GmlParserState.ReadingAttributeName;
                            break;
                        }
                        else
                        {
                            if (cursor.IsTagClose)
                            {
                                // tag closed (>), expecting text or next tag;
                                mode = GmlParserState.ReadingText;
                            }
                            else
                            {
                                // look for />
                                if (cursor.IsTagComplete)
                                {
                                    // this is a closing tag:
                                    isCloseTag = true;
                                }
                                else
                                {
                                    // keep reading the tag-name:
                                    tagName.Append(cursor.CurrentChar);

                                    // recognise the start of a comment node:
                                    if (tagName.ToString().Equals("!--"))
                                    {
                                        mode = GmlParserState.ReadingComment;
                                    }
                                }
                            }
                        }
                        break;

                    case GmlParserState.ReadingComment:

                        // at the end of the comment?
                        if (cursor.EndComment)
                        {
                            if (commentText.ToString().Trim().Length > 0)
                                // add the comment node:
                                nodes.Add(current.AddComment(commentText.ToString()));

                            // reset the comment text & tag name:
                            commentText.Clear();

                            // clear the tag name:
                            tagName.Clear();

                            // move the cursor position forward past the '-->'
                            if (cursor.Increment(2))
                                mode = GmlParserState.ReadingText;
                        }
                        else
                        {
                            // keep appending the comment text:
                            commentText.Append(cursor.CurrentChar);
                        }
                        break;

                    case GmlParserState.ReadingAttributeName:

                        // read until '=' or ' '
                        if (cursor.IsTextDelim)
                        {
                            // invert in-quotes flag:
                            inQuotes = !inQuotes;
                        }
                        else
                        {
                            // assignment operator?
                            if (cursor.CurrentChar == '=')
                                // now reading attribute value:
                                mode = GmlParserState.ReadingAttributeValue;
                            else
                            {
                                // end of a boolean attribute?
                                if (cursor.IsWhiteSpace && !inQuotes && cursor.PeekSkipWhiteSpace != '=')
                                {
                                    // ended current boolean attribute started another;
                                    if (attribName.Length > 0)
                                    {
                                        // add the attribute:
                                        attributes.Add(new TagAttribute(attribName.ToString()));
                                        attribName.Clear();
                                    }
                                }
                                else
                                {
                                    // are we at the end of a tag ? '>'g
                                    if (cursor.IsTagClose)
                                    {
                                        // move to text-content or next-tag mode:
                                        mode = GmlParserState.ReadingText;

                                        // add any pending attributes:
                                        if (attribName.Length > 0)
                                        {
                                            attributes.Add(new TagAttribute(attribName.ToString()));
                                            attribName.Clear();
                                        }
                                    }
                                    else
                                    {
                                        // are we at the end of a closing tag?
                                        if (cursor.IsTagComplete)
                                        {
                                            // set the flag indicating this is a closing tag:
                                            isCloseTag = true;
                                        }
                                        else
                                        {
                                            // keep appending to the attribute name:
                                            attribName.Append(cursor.CurrentChar);
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    case GmlParserState.ReadingAttributeValue:

                        // flip the flag on "
                        if (cursor.IsTextDelim)
                        {
                            inQuotes = !inQuotes;
                        }
                        else
                        {
                            // if inside quotes, just add to current attribute value without ado
                            if (inQuotes)
                                attribValue.Append(cursor.CurrentChar);
                            else
                            {
                                // end of a tag? >
                                if (cursor.IsTagClose)
                                {
                                    // finished reading the tag:
                                    mode = GmlParserState.ReadingText;
                                }
                                else
                                {
                                    // whitespace outside text-quotes: end of value
                                    if (cursor.IsWhiteSpace)
                                    {
                                        // space before next attribute;
                                        mode = GmlParserState.ReadingAttributeName;
                                    }
                                }
                            }
                        }

                        break;

                    case GmlParserState.ReadingText:

                        if (cursor.IsTagOpen)
                        {
                            mode = GmlParserState.ReadingTag;
                        }
                        else
                            text.Append(cursor.CurrentChar);

                        break;
                }

                // handle mode change:
                if (last != mode)
                {
                    // mode switched;
                    // if the last mode was attribute-value, then we have read a complete attribute:
                    if (last == GmlParserState.ReadingAttributeValue)
                    {
                        // completed reading attribute:
                        attributes.Add(new TagAttribute(attribName.ToString(), attribValue.ToString()));

                        // reset the attribute builders:
                        attribName.Clear();
                        attribValue.Clear();
                    }

                    // if moved from reading text to reading tag, completed a text-node:
                    if (mode == GmlParserState.ReadingTag && last == GmlParserState.ReadingText)
                    {
                        // apply the text to the current node:
                        if (text.ToString().Trim().Length > 0)
                        {
                            nodes.Add(current.AddText(text.ToString()));
                        }

                        // clear the text-value buffer:
                        text.Clear();
                    }

                    // if moved to reading-text, completed a tag: (exclude comment tags)
                    if (mode == GmlParserState.ReadingText && last != GmlParserState.ReadingComment)
                    {
                        // if the tag-name starts with '!' it will be a single-line tag:
                        if (tagName[0] == '!')
                        {
                            // inline node; add to the current node, but don't change the current node:
                            nodes.Add(current.Add(tagName.ToString(), attributes.ToArray()));
                        }
                        else
                        {
                            // completed a tag and about to read inner text or next tag;
                            // is this a closing tag? </head> ../>
                            if (isCloseTag || !cursor.HasClosingTag(tagName.ToString()))
                            {
                                // need to fall back to this tag;
                                // the current tag should be the same name as the closing tag:
                                if (current.Name.Equals(tagName.ToString()))
                                    // go up one level:
                                    current = (GmlElement)current.Parent;
                                else
                                {
                                    // open and close tag without changing the current tag;
                                    nodes.Add(current.Add(tagName.ToString(), attributes.ToArray()));
                                }
                            }
                            else
                            {
                                // just finished reading a tag;
                                // add it as a child of the current node.
                                // change the current node to be the new node;
                                current = current.Add(tagName.ToString(), attributes.ToArray());

                                // keep in the nodes list:
                                nodes.Add(current);
                            }
                        }

                        // reset tag switches and builders:
                        isCloseTag = false;
                        tagName.Clear();
                        attributes.Clear();
                    }
                }

                // record the current mode: & advance to the next byte.
                last = mode;
            }

            // return the as-built document.
            return doc;
        }




    }

    /// <summary>
    /// represents a general markup language document. could be XML, XAML, HTML, XHTML, AML etc
    /// </summary>
    public class GmlDocument : GmlElement, IEnumerable<GmlNode>
    {
        public GmlDocument()
            : base("Document")
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        protected GmlDocument(string name)
            : base(name)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="attributes"></param>
        protected GmlDocument(string name, params TagAttribute[] attributes)
            : base(name, attributes)
        {
        }

        /// <summary>
        /// gets/sets the string encoding the file was saved in, would be saved in
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;


        #region Create Nodes

        /// <summary>
        /// creates a doctype node of the given type for the given parent.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public GmlElement AddDocType(string type)
        {
            return this.Add("!DOCTYPE", new TagAttribute(type));
        }

        #endregion Create Nodes

        #region Enumerate Descendants

        public IEnumerator<GmlNode> GetEnumerator()
        {
            return Descendants.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Descendants.GetEnumerator();
        }

        #endregion Enumerate Descendants

        /// <summary>
        ///
        /// </summary>
        /// <param name="markup"></param>
        /// <returns></returns>
        public static GmlDocument Parse(Stream markup)
        {
            return GmlParser.Parse(markup);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="markup"></param>
        /// <returns></returns>
        public static GmlDocument Parse(string markup)
        {
            return GmlParser.Parse(markup);
        }

        /// <summary>
        /// open document from file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static GmlDocument Open(string fileName)
        {
            using (var fs = File.OpenRead(fileName))
            {
                return GmlParser.Parse(fs, new GmlDocument("DOCUMENT", new TagAttribute("FileName", fileName)));
            }
        }

        public override string Markup
        {
            get
            {
                // ignore the top document node.
                return base.InnerMarkup;
            }
        }

        /// <summary>
        /// write document to file.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="fileName"></param>
        public static void Save(GmlDocument doc, string fileName)
        {
            File.WriteAllText(fileName, doc.InnerMarkup, doc.Encoding);
        }
    }


    /// <summary>
    /// a slightly specialized version of a gml-document, defines HTML, HEAD and BODY elements.
    /// </summary>
    public class HtmlDocument : GmlDocument
    {
        /// <summary>
        ///
        /// </summary>
        public HtmlDocument()
            : base("HTML")
        {
            this.HTML = this;
            this.HEAD = this.HTML.AddElement(new GmlElement("HEAD"));
            this.BODY = this.HTML.AddElement(new GmlElement("BODY"));
        }

        /// <summary>
        /// gets the HTML element
        /// </summary>
        public GmlElement HTML { get; protected set; }

        /// <summary>
        /// gets the HEAD element
        /// </summary>
        public GmlElement HEAD { get; protected set; }

        /// <summary>
        /// gets the BODY element;
        /// </summary>
        public GmlElement BODY { get; protected set; }
    }

    #endregion Parser

    #region Formatter

    /// <summary>
    /// used to format GmlElements to render text or to a stream etc. The core methods to write the output are abstract.
    /// </summary>
    public abstract class GmlFormatter
    {
        #region Fields

        /// <summary>
        /// a stack of opened elements.
        /// </summary>
        protected Stack<string> _openElements = new Stack<string>();

        /// <summary>
        /// list of tags that can be loose-closed.
        /// </summary>
        protected string[] _looseCloseTags = new[] {
            "hr","br", "!DOCTYPE", "p","li"
        };

        /// <summary>
        /// is the current 'position' the start of a new line?
        /// </summary>
        protected bool _newLine = true;

        /// <summary>
        /// the character (or characters) used to create the indent.
        /// </summary>
        protected string _indentChars = "\t";

        #endregion Fields

        #region Abstract

        /// <summary>
        /// gets the HTML thus created.
        /// </summary>
        public abstract string Markup
        {
            get;
        }

        /// <summary>
        /// method to append the specified string to the output
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected abstract GmlFormatter _Append(string text);

        /// <summary>
        /// method to append the specified string and newline character to the output
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected abstract GmlFormatter _AppendLine(string text = null);

        #endregion Abstract

        #region Properties

        /// <summary>
        /// gets the string required to indent the current line, by counting the number of open elements currently in the document.
        /// </summary>
        protected string CurrentIndent
        {
            get
            {
                string str;
                if (this._openElements.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < this._openElements.Count; i++)
                    {
                        sb.Append(this._indentChars);
                    }
                    str = sb.ToString();
                }
                else
                {
                    str = "";
                }
                return str;
            }
        }

        /// <summary>
        /// peek at the currently open element.
        /// </summary>
        public string CurrentOpenElementTag
        {
            get
            {
                return this._openElements.Peek();
            }
        }

        /// <summary>
        /// determines if there is a current, open element.
        /// </summary>
        public bool IsElementOpen
        {
            get
            {
                return this._openElements.Count > 0;
            }
        }

        /// <summary>
        /// gets the number of open elements.
        /// </summary>
        public int OpenElements
        {
            get
            {
                return this._openElements.Count;
            }
        }

        #endregion Properties

        #region Indent/Formatting Methods

        /// <summary>
        /// moves to the start of a new line (if not already at the start of a new line)
        /// </summary>
        public GmlFormatter NewLine()
        {
            return (this._newLine ? this : this.AppendLine());
        }

        /// <summary>
        /// adds \r\n to the current line and sets _newLine to true.
        /// </summary>
        /// <returns></returns>
        protected GmlFormatter AppendLine()
        {
            this._AppendLine();
            this._newLine = true;
            return this;
        }

        /// <summary>
        /// add the correct number of spaces to indent the current line (if at the start of a new line);
        /// </summary>
        /// <returns></returns>
        protected GmlFormatter Indent()
        {
            if (this._newLine)
            {
                this._Append(this.CurrentIndent);
                this._newLine = false;
            }
            return this;
        }

        #endregion Indent/Formatting Methods

        /// <summary>
        /// determines if a particular tag can use the loose close rule (like Hr and Br)
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        protected bool CanLooseClose(string tagName)
        {
            if (_looseCloseTags.Contains(tagName, StringComparer.OrdinalIgnoreCase))
                return true;

            return false;
        }

        #region Append Text/Markup

        /// <summary>
        /// append a text value inside the current element.
        /// </summary>
        /// <param name="text"></param>
        public GmlFormatter AppendText(string text, bool nocheck = false)
        {
            text = text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                if (this._openElements.Count == 0)
                {
                    if (!nocheck)
                        throw new ArgumentException("No open element: cannot append text");
                }

                // break the text into lines:
                string[] lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                // one line: just append:
                if (lines.Length <= 1)
                {
                    Indent();
                    _Append(text);
                }
                else
                {
                    // add each line indented:
                    for (int l = 0; l < lines.Length; l++)
                    {
                        Indent();
                        _Append(lines[l]);
                        if (l + 1 < lines.Length)
                        {
                            NewLine();
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// appends HTML
        /// </summary>
        /// <param name="ml"></param>
        /// <returns></returns>
        public GmlFormatter AppendML(string ml)
        {
            if (!string.IsNullOrEmpty(ml))
            {
                // break the text into lines:
                string[] lines = ml.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                // one line: just append:
                if (lines.Length <= 1)
                {
                    Indent(); _Append(ml.Trim());
                }
                else
                {
                    // add each line indented:
                    for (int l = 0; l < lines.Length; l++)
                    {
                        Indent();
                        _Append(lines[l].TrimStart());
                        if (l + 1 < lines.Length)
                        {
                            NewLine();
                        }
                    }
                }
            }

            return this;
        }

        #endregion Append Text/Markup

        /// <summary>
        /// closes the last tag opened and pops it off the open-tags stack.
        /// </summary>
        public GmlFormatter Close()
        {
            if (this.IsElementOpen)
            {
                // get the tag:
                string lastTag = this._openElements.Pop();

                // end the current line & indent:
                this.NewLine(); this.Indent();

                // add the closing tag:
                this._Append("</")._Append(lastTag)._Append(">"); _newLine = false;

                // end the line:
                this.NewLine();
            }
            return this;
        }

        /// <summary>
        /// closes the last tag without starting a new line (for tags like &lt;li&gt;item&lt;/li&gt;)
        /// </summary>
        public GmlFormatter CloseInline()
        {
            if (this.IsElementOpen)
            {
                string lastTag = this._openElements.Pop();
                _Append("</")._Append(lastTag)._Append(">");
            }
            return this;
        }

        /// <summary>
        /// closes out until there are no open <paramref name="tag"/> elements.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public GmlFormatter CloseToTag(string tag)
        {
            while (true)
            {
                if ((!this.IsElementOpen ? true : !this._openElements.Contains(tag)))
                {
                    break;
                }
                this.Close();
            }
            return this;
        }

        /// <summary>
        /// insert a &lt;!-- comment --&gt;
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public GmlFormatter Comment(string comment)
        {
            this.NewLine();
            this.Indent();
            this._Append("<!--");
            this.AppendML(comment.Trim());
            this._Append("-->");
            return this.AppendLine();
        }

        /// <summary>
        /// closes any remaining, open tags.
        /// </summary>
        public GmlFormatter CloseAll()
        {
            while (this.IsElementOpen)
            {
                this.Close();
            }
            return this;
        }

        /// <summary>
        /// open an element in the document.
        /// </summary>
        /// <param name="tag">the value of the element tag (eg HTML)</param>
        /// <param name="appendLine">append a new line at the end of the tag?</param>
        /// <param name="attributes">tag properties to add, eg border="1"</param>
        public GmlFormatter Open(string tag, params TagAttribute[] attributes)
        {
            if (!this._newLine)
            {
                this.AppendLine();
                this.Indent();
            }
            else
            {
                this.Indent();
            }

            // append the tag:
            _Append("<")._Append(tag)._Append(TagAttribute.ToMarkupString(attributes))._Append(">");

            // push the tag-name into the open elements stack:
            this._openElements.Push(tag);

            // end the line and return the ref:
            return AppendLine();
        }

        /// <summary>
        /// open an element in the document.
        /// </summary>
        /// <param name="tag">the value of the element tag (eg HTML)</param>
        /// <param name="appendLine">append a new line at the end of the tag?</param>
        /// <param name="attributes">tag properties to add, eg border="1"</param>
        public GmlFormatter OpenInline(string tag, params TagAttribute[] attributes)
        {
            Indent();
            _Append("<"); // open the tag;
            _Append(tag); // append the name;
            _Append(TagAttribute.ToMarkupString(attributes)); // append the attribtues:
            _Append(">"); // close the tag:
            _openElements.Push(tag); // push the tag-name in to the open-element list

            // return the builder ref:
            return this;
        }

        /// <summary>
        /// open an element in the document.
        /// </summary>
        /// <param name="tag">the value of the element tag (eg HTML)</param>
        /// <param name="appendLine">append a new line at the end of the tag?</param>
        /// <param name="attributes">tag properties to add, eg border="1"</param>
        public GmlFormatter OpenInline(string tag, bool closeInStartTag, params TagAttribute[] attributes)
        {
            Indent();
            _Append("<"); // open the tag;
            _Append(tag); // append the name;
            _Append(TagAttribute.ToMarkupString(attributes)); // append the attributes (if any)

            if (closeInStartTag)
            {
                if (CanLooseClose(tag))
                {
                    // loose-close:
                    _Append(">");
                }
                else
                {
                    // proper close:
                    _Append("/>");
                }

                if (attributes.Length > 0)
                    NewLine();
            }
            else
            {
                _Append(">");               // close the tag:
                _openElements.Push(tag);    // push the tag-name into the open-element list
            }

            // return the builder ref:
            return this;
        }
    }

    /// <summary>
    /// a GmlFormatter implementation that writes to a string-builder.
    /// </summary>
    public class GmlStringFormatter : GmlFormatter
    {
        /// <summary>
        /// the document
        /// </summary>
        protected StringBuilder _doc = null;

        public GmlStringFormatter()
        {
            _doc = new StringBuilder();
        }

        public GmlStringFormatter(StringBuilder builder)
        {
            _doc = builder;
        }

        protected override GmlFormatter _Append(string text)
        {
            _doc.Append(text);
            return this;
        }

        protected override GmlFormatter _AppendLine(string text = null)
        {
            _doc.AppendLine(text);
            return this;
        }

        public override string Markup
        {
            get { return _doc.ToString(); }
        }
    }

    /// <summary>
    /// implementation of the formatter for writing to stream.
    /// </summary>
    public class GmlStreamFormatter : GmlFormatter
    {
        protected Encoding _encoding = Encoding.UTF8;
        protected StringBuilder _copy = new StringBuilder();
        protected Stream _target = null;

        public GmlStreamFormatter(Stream target)
        {
            _target = target;
        }

        public override string Markup
        {
            get { return _copy.ToString(); }
        }

        protected override GmlFormatter _Append(string text)
        {
            byte[] data = _encoding.GetBytes(text);
            _target.Write(data, 0, data.Length);
            _copy.Append(text);
            return this;
        }

        protected override GmlFormatter _AppendLine(string text = null)
        {
            if (text == null)
                return this;
            return _Append(text + "\r\n");
        }
    }

    #endregion Formatter

    #region Attributes

    /// <summary>
    /// a class holding an attribute for a gml tag/element.
    /// </summary>
    public class TagAttribute
    {
        /// <summary>
        /// gets or sets the name of the property
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// gets or sets the value of the property.
        /// </summary>
        public string Value
        {
            get;
            set;
        }

        /// <summary>
        /// empty constructor
        /// </summary>
        public TagAttribute()
        {
        }

        /// <summary>
        /// construct boolean attribute.
        /// </summary>
        /// <param name="name"></param>
        public TagAttribute(string name)
        {
            this.Name = name;
            this.Value = null;
        }

        /// <summary>
        /// construct with name and value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public TagAttribute(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// returns the string property assignment.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this.Value == null)
            {
                // this is a boolean type;
                if (this.Name.Contains(" "))
                {
                    // which needs quotes:
                    return '"' + this.Name + '"';
                }
                else
                {
                    return this.Name;
                }
            }
            else
            {
                string s = this.Name + "=\"" + this.Value + "\"";
                return s;
            }
        }

        /// <summary>
        /// helper method to create a class element attribute (class ="classname")
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public static TagAttribute GetClass(string className)
        {
            return new TagAttribute("class", className);
        }

        public static TagAttribute GetName(string name)
        {
            return new TagAttribute("name", name);
        }

        public static TagAttribute GetHref(string href)
        {
            return new TagAttribute("href", href);
        }

        public static TagAttribute GetId(string id)
        {
            return new TagAttribute("id", id);
        }

        /// <summary>
        /// enables the creation of a series of element-attribute objects, using each pair of strings entered into the parameter array.
        /// </summary>
        /// <param name="valuePairs"></param>
        /// <returns></returns>
        public static TagAttribute[] List(params string[] valuePairs)
        {
            List<TagAttribute> list = new List<TagAttribute>();
            for (int i = 0; i < (int)valuePairs.Length; i = i + 2)
            {
                if (i + 1 >= (int)valuePairs.Length)
                {
                    list.Add(new TagAttribute(valuePairs[i]));
                }
                else
                {
                    list.Add(new TagAttribute(valuePairs[i], valuePairs[i + 1]));
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// renders the enumeration of attributes and values to a string.
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public static string ToMarkupString(IEnumerable<TagAttribute> attributes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var a in attributes)
            {
                sb.Append(' ');
                sb.Append(a.ToString());
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// factory class for producing element-attributes.
    /// </summary>
    public static class GlobalEventAttributes
    {
        /// <summary>
        /// list of mouse events
        /// </summary>
        public enum MouseEvents { onclick, ondblclick, ondrag, ondragend, ondragenter, ondragleave, ondragover, ondragstart, ondrop, onmousedown, onmousemove, onmouseout, onmouseover, onmouseup, onmousewheel, onscroll }

        /// <summary>
        /// list of form events
        /// </summary>
        public enum FormEvents
        {
            onblur,
            onchange,
            oncontextmenu,
            onfocus,
            onformchange,
            onforminput,
            oninput,
            oninvalid,
            onselect,
            onsubmit
        }

        public enum WindowEvents
        {
            onafterprint,
            onbeforeprint,
            onbeforeunload,
            onerror,
            onhaschange,
            onload,
            onmessage,
            onoffline,
            onpagehide,
            onpageshow,
            onpopstate,
            onredo,
            onresize,
            onstorage,
            onundo,
            onunload
        }

        public enum KeyboardEvents
        {
            onkeydown, onkeyup, onkeypress
        }

        private static TagAttribute CreateEventAttribute(Enum eventType, string value)
        {
            return new TagAttribute(eventType.ToString(), value);
        }

        public static TagAttribute Window(WindowEvents evt, string script)
        {
            return CreateEventAttribute(evt, script);
        }

        public static TagAttribute Form(FormEvents evt, string script)
        {
            return CreateEventAttribute(evt, script);
        }

        public static TagAttribute KeyBoard(KeyboardEvents evt, string script)
        {
            return CreateEventAttribute(evt, script);
        }

        public static TagAttribute Mouse(MouseEvents evt, string script)
        {
            return CreateEventAttribute(evt, script);
        }
    }

    /// <summary>
    /// button types
    /// </summary>
    public enum ButtonType { button, reset, submit }

    /// <summary>
    /// form-target values
    /// </summary>
    public enum FormTarget { _blank, _self, _parent, _top }

    /// <summary>
    /// form method values
    /// </summary>
    public enum FormMethod { get, post }

    #endregion Attributes

    #region Other

    /// <summary>
    /// simple type to use for complex item inputs.
    /// </summary>
    public class GmlItem
    {
        #region Properties

        /// <summary>
        /// the value or term.
        /// </summary>
        public string Value
        {
            get;
            set;
        }

        /// <summary>
        /// the description.
        /// </summary>
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// any attributes to go with the item.
        /// </summary>
        public List<TagAttribute> Attributes { get; set; }

        #endregion Properties

        #region Ctor

        /// <summary>
        /// constructor
        /// </summary>
        public GmlItem()
        {
            this.Attributes = new List<TagAttribute>();
        }

        public GmlItem(string item, string definition, params TagAttribute[] attributes)
        {
            this.Value = item;
            this.Description = definition;
            this.Attributes = new List<TagAttribute>(attributes);
        }

        #endregion Ctor

        public static explicit operator TagAttribute(GmlItem item)
        {
            return new TagAttribute(item.Value, item.Description);
        }

        public static implicit operator GmlItem(TagAttribute att)
        {
            return new GmlItem(att.Name, att.Value);
        }
    }

    /// <summary>
    /// markup table builder using nodes;
    /// </summary>
    public class GmlTable
    {
        /// <summary>
        /// table-row node; represents a row-header for a HTML table;
        /// </summary>
        public class TR : GmlElement
        {
            /// <summary>
            /// table header element. (column header)
            /// </summary>
            public class TH : GmlElement
            {
                public TH(TR row)
                    : base()
                {
                    this.Parent = row;
                    this.Name = "th";
                }

                public TH(TR row, string text, params TagAttribute[] attribs)
                    : base()
                {
                    this.Parent = row;
                    this.Name = "th";
                    this.Attributes.AddRange(attribs);
                    this.Children.Add(new GmlText(this, text));
                }
            }

            /// <summary>
            /// table definition element (cell)
            /// </summary>
            public class TD : GmlElement
            {
                public TD(TR row)
                    : base()
                {
                    this.Parent = row;
                    this.Name = "td";
                }

                public TD(TR row, string value, params TagAttribute[] attributes)
                    : base()
                {
                    this.Parent = row;
                    this.Name = "td";
                    this.Attributes.AddRange(attributes);
                    this.Children.Add(new GmlText(this, value));
                }
            }

            /// <summary>
            /// construct the row;
            /// </summary>
            /// <param name="table"></param>
            /// <param name="attribs"></param>
            public TR(GmlElement table, params TagAttribute[] attribs)
            {
                this.Parent = table;
                this.Name = "tr";
                this.Attributes.AddRange(attribs);
            }

            /// <summary>
            /// gets or sets if this row-header is a column-header  row.
            /// </summary>
            public bool IsHeaderRow { get; set; }

            /// <summary>
            /// enumerates the TD nodes within the current row;
            /// </summary>
            public IEnumerable<TD> TDValues { get { return (from c in this.Children where c is TD select c as TD); } }

            /// <summary>
            /// enumerates the TH nodes within the current row;
            /// </summary>
            public IEnumerable<TH> THValues { get { return (from c in this.Children where c is TH select c as TH); } }

            /// <summary>
            /// returns the cells in the current row.
            /// </summary>
            public IEnumerable<GmlElement> Cells
            {
                get
                {
                    if (this.IsHeaderRow)
                        return THValues;
                    else
                        return TDValues;
                }
            }

            /// <summary>
            /// adds a table-header into the row;
            /// </summary>
            /// <returns></returns>
            public TH AddTH()
            {
                return (TH)AddElement(new TH(this));
            }

            /// <summary>
            /// adds a cell into the row;
            /// </summary>
            /// <returns></returns>
            public TD AddTD()
            {
                return (TD)AddElement(new TD(this));
            }

            /// <summary>
            /// adds a value to the current row;
            /// if the row is the header-row, then this will add a TH element, else a TD element.
            /// </summary>
            /// <param name="value"></param>
            /// <param name="attributes"></param>
            /// <returns></returns>
            public GmlElement AddRowValue(string value = null, params TagAttribute[] attributes)
            {
                GmlElement node = null;
                if (this.IsHeaderRow)
                {
                    node = new TH(this);
                    node.Attributes.AddRange(attributes);
                    if (!string.IsNullOrEmpty(value))
                    {
                        node.Children.Add(new GmlText(this, value));
                    }
                }
                else
                {
                    node = new TD(this);
                    node.Attributes.AddRange(attributes);
                    if (!string.IsNullOrEmpty(value))
                    {
                        node.Children.Add(new GmlText(this, value));
                    }
                }
                return AddElement(node);
            }
        }

        #region Fields

        /// <summary>
        /// the table node (top level of the table)
        /// </summary>
        private GmlElement _tableNode = null;

        /// <summary>
        /// the header-row node;
        /// </summary>
        private TR _headerRow = null;

        /// <summary>
        /// holds row nodes;
        /// </summary>
        private List<TR> _rows = new List<TR>();

        /// <summary>
        /// list of row-classes to cycle through
        /// </summary>
        private List<string> _rowClassCycleList = new List<string>(new[] { "row_even", "row_odd" });

        #endregion Fields

        #region Constructor

        /// <summary>
        /// construct the table, supplying an owner-node and attributes for the table tag.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="attribs"></param>
        public GmlTable(GmlNode owner, params TagAttribute[] attribs)
        {
            _tableNode = new GmlElement(owner, "table", attribs);
        }

        /// <summary>
        /// constuct the table with attributes.
        /// </summary>
        /// <param name="attributes"></param>
        public GmlTable(params TagAttribute[] attributes)
        {
            _tableNode = new GmlElement("table", attributes);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cClass"></param>
        /// <param name="borders"></param>
        /// <param name="attibutes"></param>
        public GmlTable(string id, string cClass, string borders, params TagAttribute[] attibutes)
            : this(attibutes)
        {
            if (_tableNode != null)
            {
                // table node already created, add the extra attributes:
                _tableNode.Attributes.Add(TagAttribute.GetId(id));
                _tableNode.Attributes.Add(TagAttribute.GetClass(cClass));
                _tableNode.Attributes.Add(new TagAttribute("border", borders));
            }
        }

        #endregion Constructor

        #region Add Row

        /// <summary>
        /// adds a row header to the table. add cells to the table-row;
        /// </summary>
        /// <param name="attr"></param>
        /// <returns></returns>
        public TR AddRow(params TagAttribute[] attr)
        {
            // is this a header-row:
            bool headerRow = this.HasHeaderRow && this.Rows.Count() == 0;

            // this will be the index for the row:
            int row_num = _rows.Count;

            // create the row:
            var row = new TR(_tableNode, attr);

            // assign the cyclic row-class:
            if (_rowClassCycleList.Count > 0)
            {
                // get the class:
                string row_class = _rowClassCycleList[row_num % _rowClassCycleList.Count];

                // set the class attribute on the row:
                if (row["class"] == null)
                {
                    row["class"] = new TagAttribute("class", row_class);
                }
            }
            if (headerRow)
            {
                // set if it's the header row:
                row.IsHeaderRow = true;

                // assign to the header-row field:
                _headerRow = row;
            }

            // add the row to the table-node:
            row.SetParent(this._tableNode);

            // store the row in the collection:
            this._rows.Add(row);

            // return it;
            return row;
        }

        /// <summary>
        /// creates a new row in the table and adds a value for each column.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public TR AddRowValues(params string[] values)
        {
            // create the row:
            var tr = AddRow();

            // add each value:
            foreach (var val in values)
            {
                tr.AddRowValue(val);
            }

            // return the row-header;
            return tr;
        }

        /// <summary>
        /// add row values using the HtmlItem structures to supply value and attributes for each column in the row.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public TR AddRowValues(params GmlItem[] values)
        {
            // create the row:
            var tr = AddRow();

            foreach (var val in values)
            {
                var cell = tr.AddRowValue(val.Value, val.Attributes.ToArray());
            }

            return tr;
        }

        #endregion Add Row

        #region Properties

        /// <summary>
        /// gets or sets if the table has a header row.
        /// </summary>
        public bool HasHeaderRow { get; set; }

        /// <summary>
        /// gets the table node;
        /// </summary>
        public GmlElement TableNode { get { return _tableNode; } }

        /// <summary>
        /// gets the header row node;
        /// </summary>
        public TR HeaderRow { get { return _headerRow; } }

        /// <summary>
        /// enumerate the rows in the table;
        /// </summary>
        public IEnumerable<TR> Rows
        {
            get
            {
                return (from c in _rows select c);
            }
        }

        /// <summary>
        /// gets the markup representation of the table.
        /// </summary>
        public string Markup
        {
            get { return _tableNode.Markup; }
        }

        #endregion Properties

        /// <summary>
        /// adds the table markup into a formatter:
        /// </summary>
        /// <param name="b"></param>
        public void Format(GmlFormatter b)
        {
            _tableNode.RenderMarkup(b);
        }

        /// <summary>
        /// inserts spaces where the case changes from lower to upper.
        /// ie: TheQuickBrownFox becomes The Quick Brown Fox.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string ProcessName(string name)
        {
            // process a camel-cased name;
            // insert spaces each time a character goes to upper case from lower:
            var sb = new StringBuilder();
            var lowerCase = false;
            foreach (var c in name)
            {
                if (Char.IsUpper(c) && lowerCase)
                {
                    sb.Append(' ');
                }
                lowerCase = Char.IsLower(c);
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static GmlTable FromReflection(Object subject)
        {
            var tbl = new GmlTable(subject.GetType().Name, "propTable", "0");

            foreach (var p in subject.GetType().GetProperties())
            {
                if (p.CanRead && !p.GetGetMethod().IsStatic)
                {
                    var row = tbl.AddRow();
                    row.AddRowValue(ProcessName(p.Name));
                    row.AddRowValue(p.GetValue(subject, null).ToString());
                }
            }

            return tbl;
        }

        /// <summary>
        /// populates an abstracted GmlTable with details of the exception.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        public static GmlTable FromException(Exception ex)
        {
            var master = new GmlTable("exception", "tbl", "0");
            int indent = -1;

            var stack = new Stack<Exception>(); var temp = ex;
            while (temp != null)
            {
                stack.Push(temp);
                temp = temp.InnerException;
            }

            while (stack.Count > 0)
            {
                var tbl = new GmlTable();
                var current = stack.Pop(); indent++;
                var row = tbl.AddRow();

                // create the indent string:
                var sbi = new StringBuilder();
                for (int i = 0; i < indent; i++)
                    sbi.Append("&gt; ");
                var instr = sbi.ToString();

                // add a row describing the exception type:
                row.AddRowValue(instr);
                row.AddRowValue(current.GetType().Description(true) + " in [" + Path.GetFileName(current.GetType().Module.FullyQualifiedName) + "]");

                // specify the message:
                row = tbl.AddRow();
                row.AddRowValue(instr);
                row.AddRowValue("<b>" + current.Message + "</b>");

                // include the stack trace:
                row = tbl.AddRow();
                row.AddTD();
                row.AddTD().AddElement(FromStackTrace(new StackTrace(current, true), null, instr).TableNode);

                master.AddRow().AddTD().AddElement(tbl.TableNode);

            }
            return master;
        }

        public static GmlElement FormatException(Exception e)
        {
            var element = new GmlElement("code");
            if (e != null)
            {
                var trace = new StackTrace(e, true);
                var type = e.GetType();
                var modl = trace.GetFrame(0).GetMethod().Module;
                var list = element.P(type.Description(true) + " in [" + Path.GetFileName(modl.FullyQualifiedName) + "]").DL();

                // add the  message:
                var dt = list.DT();
                dt.AddText("Message:");
                dt.I(e.Message);

                // add a stack trace:
                var dd = list.DD();
                dd.AddElement(GmlTable.FromStackTrace(trace, null, " at ").TableNode);

                // recurse through the inner exception:
                if (e.InnerException != null)
                {
                    list.DT("Caused By:");
                    list.DD().AddElement(FormatException(e.InnerException));
                }
            }

            return element;
        }

        /// <summary>
        /// formats a stack-trace in HTML using a table to align the elements.
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        public static GmlTable FromStackTrace(StackTrace trace, GmlTable tbl = null, string indent = "")
        {
            if (tbl == null)
            {
                tbl = new GmlTable();
            }
            var frames = trace.GetFrames(); int i = 0;
            if (frames != null)
            {
                foreach (var stackFrame in frames)
                {
                    var method = stackFrame.GetMethod();
                    var type = method.DeclaringType;
                    var line = stackFrame.GetFileLineNumber();
                    var codeFileName = stackFrame.GetFileName();
                    var file = System.IO.Path.GetFileName(codeFileName);

                    // create a row for the stack frame:
                    var row = tbl.AddRow();

                    string modifier = null;
                    if (method.IsStatic)
                        modifier = "[static]";
                    else
                        modifier = "[inst]";

                    // add each part of the stack trace to the table:
                    row.AddRowValue(indent);
                    row.AddRowValue("[" + i++ + "]");
                    row.AddRowValue(method.Description(true));
                    row.AddRowValue(modifier);
                    if (file != null)
                    {
                        row.AddRowValue(file);
                        row.AddRowValue("Line:" + line.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        row.AddRowValue(); row.AddRowValue();
                    }
                }
            }
            return tbl;
        }

        /// <summary>
        /// creates a markup table from the data-table;
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static GmlTable FromDataTable(DataTable table)
        {
            // create the markup table:
            var tbl = new GmlTable(table.TableName, "DataTable", "1") { HasHeaderRow = true };

            // create the header-row:
            var headerRow = tbl.AddRow();

            // populate the column names into the header row:
            foreach (DataColumn col in table.Columns)
            {
                // add the column-names into the header row:
                headerRow.AddRowValue(col.ColumnName);
            }

            // enumerate the data-rows:
            foreach (DataRow row in table.Rows)
            {
                // create a row in the markup table:
                var rowNode = tbl.AddRow();

                // set the value of each columnL
                foreach (DataColumn col in table.Columns)
                {
                    // get the value:
                    var value = row[col];

                    // is it null?
                    if (value != null)
                    {
                        // add the string expression of the value:
                        rowNode.AddRowValue(value.ToString());
                    }
                    else
                    {
                        // add a zer0-length string for a null:
                        rowNode.AddRowValue("");
                    }
                }
            }

            return tbl;
        }

        /// <summary>
        /// creates a definition table from the list of items.
        /// table has two columns, first column formatted bold and values are suffixed with ":"
        ///
        /// </summary>
        /// <param name="definitionItems"></param>
        /// <returns></returns>
        public static GmlTable DefinitionTbl(params GmlItem[] definitionItems)
        {
            // create the table:
            GmlTable tbl = new GmlTable();

            // append a border="" attribute to the table-node:
            tbl.TableNode.Attributes.Add(new TagAttribute("border", "0"));

            // iterate the items to add:
            foreach (var item in definitionItems)
            {
                // add a row: use the attributes from the item for the table-row element:
                var row = tbl.AddRow(item.Attributes.ToArray());

                // add the first column:
                var itemCol = row.AddTD();

                // set the inline style on the first column:
                itemCol.StyleAttribute = "text-align: right; ";

                // set the text on the first column:
                itemCol.B().AddText(item.Value);

                // now add the second column (the description)
                var descCol = row.AddTD();

                // set the inline style on the second column:
                descCol.StyleAttribute = "text-align: justify;";

                // add the text to the second col:
                descCol.AddText(item.Description);
            }

            // return the table definition:
            return tbl;
        }
    }

    public static class Ext
    {
        public static string Description(this MethodBase method, bool verbose = false)
        {
            // create a description of the method-info;
            var sb = new StringBuilder();

            sb.Append(method.DeclaringType.Description(verbose)).Append('.').Append(method.Name);

            int gi = 0; var gargs = method.GetGenericArguments();
            if (gargs.Length > 0)
            {
                sb.Append("&lt;");
                foreach (var gp in gargs)
                {
                    if (gi++ > 0)
                        sb.Append(", ");
                    sb.Append(gp.Description());
                }
                sb.Append("&gt;");
            }

            sb.Append('(');
            foreach (var param in method.GetParameters())
            {
                if (param.Position > 0)
                    sb.Append(", ");

                sb.Append(param.Description());
            }
            sb.Append(')');

            return sb.ToString();
        }

        public static string Description(this ParameterInfo param)
        {
            var sb = new StringBuilder();
            if (param.IsOut)
                sb.Append("[out] ");
            sb.Append(param.ParameterType.Description());
            sb.Append(' ');
            sb.Append(param.Name);

            return sb.ToString();
        }

        public static string Description(this Type type, bool verbose = false)
        {
            StringBuilder sb = new StringBuilder();
            if (verbose)
                sb.Append(type.FullName ?? type.Name);
            else
                sb.Append(type.Name);

            if (type.GetGenericArguments().Length > 0)
            {
                sb.Append("&lt;");
                for (int i = 0; i < type.GetGenericArguments().Length; i++)
                {
                    if (i > 0)
                        sb.Append(',');

                    sb.Append(type.Description(false));
                }
                sb.Append("&gt;");
            }
            if (type.IsArray)
            {
                //sb.Append("[]");
            }
            return sb.ToString();
        }
    }

    #endregion Other


}