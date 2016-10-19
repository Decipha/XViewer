using Quick.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace XdocViewer
{
	/// <summary>
	/// view-model for XDocs.
	/// </summary>
	public class XDocViewModel	: Quick.MVVM.ViewModelBase
	{
        public XDocViewModel()
        {
            this.FontSize = 12;

            if (this.InDesignMode)
            {
                this.XML = XDocument.FormatXML("<XML><Element Attribute=\"Value\"><Node>Text</Node></Element></XML>");
                this.SearchTerm = "Text";
            }
        }

        /// <summary>
        /// results of a text search
        /// </summary>
        public MatchCollection SearchResults { get; set; }

        /// <summary>
        /// define an event that the window can subscribe to to set the text highlight in the bound text box.
        /// </summary>
        public event EventHandler<Match> HighlightSearchResult;

        /// <summary>
        /// event the window subscribes to in order to perform a replacement
        /// </summary>
        public event EventHandler<Match> ReplaceSearchResult;

        protected virtual void OnHighlightMatch(Match match)
        {
            HighlightSearchResult?.Invoke(this, match);
        }

        protected virtual void OnReplaceMatch(Match match)
        {
            ReplaceSearchResult?.Invoke(this, match);
        }

        public string SearchTerm
        {
            get { return GetValue(() => SearchTerm); }
            set {
                SetValue(() => SearchTerm, value);
            }
        }

        public string ReplaceTerm
        {
            get { return GetValue(() => ReplaceTerm); }
            set { SetValue(() => ReplaceTerm, value); }
        }

        protected string SearchedTerm { get; set; }


        public int SearchResultCount
        {
            get { return GetValue(() => SearchResultCount); }
            set { SetValue(() => SearchResultCount, value); }
        }

        public int SearchResultIndex
        {
            get { return GetValue(() => SearchResultIndex); }
            set { SetValue(() => SearchResultIndex, value);

                if (SearchResultCount > 0)
                {
                    // raise the event to trigger the ui to highlight the text selected by the match
                    OnHighlightMatch(SearchResults[value]);
                }

            }
        }

        public ICommand Search {  get { return new RelayCommand(ExecSearch); } }

        protected virtual void ExecSearch(object param)
        {
            if (this.SearchTerm != this.SearchedTerm)
            {
                this.SearchResults = Regex.Matches(this.XML, this.SearchTerm);
                this.SearchResultCount = this.SearchResults.Count;
                this.SearchResultIndex = 0;
                this.SearchedTerm = this.SearchTerm;
            }
            else
            {
                if (SearchResultIndex < SearchResultCount - 1)
                    SearchResultIndex++;
                else
                    SearchResultIndex = 0;
            }
        }

        public ICommand Replace {  get { return new RelayCommand(()=> !string.IsNullOrEmpty(ReplaceTerm), ExecReplace); } }

        protected virtual void ExecReplace(object param)
        {
            ExecSearch(param);
            if (!string.IsNullOrEmpty(ReplaceTerm) && this.SearchResultCount > 0)
            {
                OnReplaceMatch(this.SearchResults[SearchResultIndex]);
                this.SearchedTerm = null;
            }
        }

        public double FontSize
        {
            get { return GetValue(() => FontSize); }
            set { SetValue(() => FontSize, value); }
        }


		/// <summary>
		/// gets or sets the current file name
		/// </summary>
		public string FileName
		{
			get { return GetValue(()=>FileName); }
			set { SetValue(()=>FileName, value); }
		}

		/// <summary>
		/// gets or sets the XML for the Xdoc.
		/// </summary>
		public string XML
		{
			get { return GetValue(() => XML); }
			set { SetValue(() => XML, value); }
		}

        /// <summary>
        /// records if the contents of the current file have been changed. set by an event handler.
        /// </summary>
        public bool IsChanged
        {
            get { return GetValue(() => IsChanged); }
            set { SetValue(() => IsChanged, value); }
        }

		/// <summary>
		/// command to open one of the supported files.
		/// </summary>
		public ICommand Open
		{
			get
			{
				return new Quick.MVVM.RelayCommand(ExecOpenFile);
			}
		}

        /// <summary>
        /// command to save as
        /// </summary>
		public ICommand SaveAs
		{
			get
			{
				return new Quick.MVVM.RelayCommand(ExecSaveAs);
			}
		}

        /// <summary>
        /// command to save (overwrite existing file)
        /// </summary>
        public ICommand Save
        {
            get
            {
                return new RelayCommand(IsSaveEnabled, ExecSave);
            }
        }

        /// <summary>
        /// copy to clipboard
        /// </summary>
		public ICommand Copy
		{
			get
			{
				return new RelayCommand(ExecCopyAll);
			}
		}

        /// <summary>
        /// paste from clipboard with XML formatting.
        /// </summary>
		public ICommand PasteAndFormat
		{
			get
			{
				return new RelayCommand(() => System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.Text), ExecPasteWithFormatting);
			}
		}


        /// <summary>
        /// paste from clipboard
        /// </summary>
        public ICommand Paste
        {
            get
            {
                return new RelayCommand(() => System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.Text), ExecPastePlain);
            }
        }

        /// <summary>
        /// flip between white on black or black on white
        /// </summary>
        public ICommand FlipColourScheme
        {
            get
            {
                return new RelayCommand(ExecFlipColours);
            }
        }

        public ICommand SetFontSmall
        {
            get
            {
                return new RelayCommand((o) => this.FontSize--);
            }
        }

        public ICommand SetFontMedium
        {
            get
            {
                return new RelayCommand((o) => this.FontSize = 12);
            }
        }

        public ICommand SetFontLarge
        {
            get
            {
                return new RelayCommand((o) => this.FontSize++);
            }
        }



        /// <summary>
        /// load a file on startup method.
        /// </summary>
        /// <param name="fileName"></param>
        public void  OnStartupOpenSupportedFile(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName);
            try
            {
                this.IsBusy = true;

                switch (ext.ToLower())
                {
                    case ".fpr":
                    case ".xdc":

                        // read as an XDOC
                        this.XML = XDocument.FormatXML(XDocument.ReadBinaryXDoc(fileName));
                        this.FileName = fileName;
                        break;

                    case ".xml":

                        // read as a text file and format
                        this.XML = XDocument.FormatXML(System.IO.File.ReadAllText(fileName), true);
                        this.FileName = fileName;
                        break;


                    default:
                        // fallback treat as a text file
                        this.XML = System.IO.File.ReadAllText(fileName);
                        this.FileName = fileName;
                        break;
                }
                
            }
            catch (Exception fail)
            {
                System.Windows.MessageBox.Show(fail.Message);
            }
            finally
            {
                this.IsBusy = false;
            }

        }

        /// <summary>
        /// command to flip the colour scheme
        /// </summary>
        /// <param name="param"></param>
        protected virtual void ExecFlipColours(object param)
        {
            if (this.Background.Equals(Brushes.White))
            {
                this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                this.Foreground = Brushes.AntiqueWhite;
            }
            else
            {
                this.Background = Brushes.White;
                this.Foreground = Brushes.Black;
            }
        }




		/// <summary>
		/// opens one of the supported file types.
		/// </summary>
		/// <param name="param">not used, exists to match Action{T} signature</param>
		protected virtual void ExecOpenFile(object param)
		{
			var dlg    = new Microsoft.Win32.OpenFileDialog();
			dlg.Title  = "Open XDoc";
			dlg.Filter = "XDOC *.xdc|*.xdc|XML (*.xml)|*.xml|KTM Projects (*.fpr)|*.fpr|Other Files (*.*)|*.*";
			var rst = dlg.ShowDialog();
			if (rst.Value)
			{
				this.FileName = dlg.FileName;
				try
				{
					// set the form to busy
					IsBusy = true;

					switch (dlg.FilterIndex)
					{
						case 1:
							// read the document, format the XML, set in the relevent field.
							this.XML = XDocument.FormatXML(XDocument.ReadBinaryXDoc(dlg.FileName));
                            this.IsChanged = false;
							break;

						case 3:
							this.XML = XDocument.FormatXML(XDocument.ReadBinaryXDoc(dlg.FileName));
                            this.IsChanged = false;
                            break;

                        case 2:
                            this.XML = XDocument.FormatXML(System.IO.File.ReadAllText(dlg.FileName));
                            this.IsChanged = false;
                            break;

                        default:
                            this.XML = XDocument.FormatXML(System.IO.File.ReadAllText(dlg.FileName), true);
                            this.IsChanged = false;
                            break;
                    }


				}
				catch (Exception e)
				{
					// something went wrong, display the message
					System.Windows.MessageBox.Show(e.Message);
				}
				finally
				{
                    // un-busy the form
					IsBusy = false;
				}
			}
		}

        /// <summary>
        /// determines if the 'Save' command should be enabled
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsSaveEnabled()
        {
            if (IsChanged)
            {
                return CanSaveOverwrite();
            }
            return false;
        }

        /// <summary>
        /// currently doesn't support writing to xdc or fpr files; check the current file-name is one that can be saved.
        /// </summary>
        /// <returns></returns>
        protected virtual bool CanSaveOverwrite()
        {
            if (!string.IsNullOrWhiteSpace(FileName))
            {
                var ext = System.IO.Path.GetExtension(this.FileName);
                switch (ext.ToLower())
                {
                    case ".xdc":
                    case ".fpr":
                        return false;

                    case ".xml":
                    case ".txt":
                    default:
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// save changes to current file
        /// </summary>
        /// <param name="param"></param>
        protected virtual void ExecSave(object param)
        {
            // double check we can overwrite this file type
            if (CanSaveOverwrite())
            {
                try
                {
                    this.IsBusy    = true;
                    System.IO.File.WriteAllText(this.FileName, this.XML);
                    this.IsChanged = false;
                }
                catch (Exception any)
                {
                    System.Windows.MessageBox.Show(any.Message);
                }
                finally
                {
                    this.IsBusy = false;
                }
            }
        }

		/// <summary>
		/// implementation for the SaveAs command
		/// </summary>
		/// <param name="param"></param>
		protected virtual void ExecSaveAs(object param)
		{
			var dlg    = new Microsoft.Win32.SaveFileDialog();
			dlg.Title  = "Save as XML";
			dlg.Filter = "XML Files (*.xml)|*.xml|Text Files (*.txt)|*.txt|Other (*.*)|*.*";
			var rst    = dlg.ShowDialog();
			if (rst.Value)
			{
				try
				{
                    // set the form to busy
					IsBusy = true;

                    // write the XML to the selected text file.
					System.IO.File.WriteAllText(dlg.FileName, XML);

                    // update the current file name
					this.FileName  = dlg.FileName;
                    this.IsChanged = false;

				}
				catch (Exception e)
				{
                    // display any error
					System.Windows.MessageBox.Show(e.Message);
				}
				finally
				{
                    // un-busy the form
					IsBusy = false;
				}
			}
		}

		/// <summary>
		/// copy the xml text to the clipboard
		/// </summary>
		/// <param name="param"></param>
		protected virtual void ExecCopyAll(object param)
		{
            try
            {
                System.Windows.Clipboard.SetText(this.XML);
            }
            catch (Exception any)
            {
                System.Windows.MessageBox.Show(any.Message);
            }
		}

		/// <summary>
		/// paste formatted xml from the clipboard.
		/// </summary>
		/// <param name="param"></param>
		protected virtual void ExecPasteWithFormatting(object param)
		{
            try
            {
                this.IsBusy = true;

                // grab any text from the clipboard
                var text = System.Windows.Clipboard.GetText();

                // try to format but not neccessarily xml;
                this.XML = XDocument.FormatXML(text, ignoreFormatError: true);

                // text has changed:
                this.IsChanged = true;
            }
            catch (Exception any)
            {
                System.Windows.MessageBox.Show(any.Message);
            }
            finally
            {
                this.IsBusy = false;
            }
		}

        /// <summary>
        /// paste formatted xml from the clipboard.
        /// </summary>
        /// <param name="param"></param>
        protected virtual void ExecPastePlain(object param)
        {
            try
            {
                this.IsBusy = true;

                // grab any text from the clipboard
                var text = System.Windows.Clipboard.GetText();

                // try to format but not neccessarily xml;
                this.XML = text;

                // text has changed:
                this.IsChanged = true;
            }
            catch (Exception any)
            {
                System.Windows.MessageBox.Show(any.Message);
            }
            finally
            {
                this.IsBusy = false;
            }
        }
    }


    public static class ExtensionMethods
    {
        /// <summary>
        /// creates a regular-expression equivalent to the specified wildcard search pattern, unless the pattern is already a regex (starts with "^" and ends with "$")
        /// thus: "*Hello*" becomes "^.*Hello.*$"
        /// </summary>
        /// <param name="pattern">the wildcard search pattern to change to a regex.</param>
        /// <returns>a regular expression equivalent to the input wildcard pattern.</returns>
        public static string ToRegex(this string pattern)
        {
            // is the pattern already a regex?
            if (pattern.StartsWith("^", StringComparison.Ordinal) && pattern.EndsWith("$", StringComparison.Ordinal))
                return pattern;
            else
                // turn the wildcard search into a regex:
                return "^" + Regex.Escape(pattern).
                             Replace(@"\*", ".*").
                             Replace(@"\?", ".") + "$";
        }

    }
}
