﻿/* Copyright 2011 the OpenDMS.NET Project (http://sites.google.com/site/opendmsnet/)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;

namespace WindowsClient
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        private Common.CouchDB.Database _couchdb;
        /// <summary>
        /// Reference to the method that handles the update ui events.
        /// </summary>
        private delegate void UpdateUI();
        public delegate void DisplayResultsDelegate(DataGrid grid, Common.CouchDB.Lucene.SearchResultCollection results);
        /// <summary>
        /// A reference to the method that handles search result selection events.
        /// </summary>
        /// <param name="guid">The <see cref="Guid"/> of the selected Asset.</param>
        public delegate void SearchResultHandler(Guid guid);
        /// <summary>
        /// Occurs when a result is selected.
        /// </summary>
        public event SearchResultHandler OnResultSelected;

        /// <summary>
        /// A collection of form properties to display to the user to allow searching.
        /// </summary>
        private Common.NetworkPackage.SearchForm _searchForm;
        /// <summary>
        /// A collection of properties bound on this GUI.
        /// </summary>
        private Dictionary<string, UIElement> _boundProperties;
        /// <summary>
        /// Width of the search column minus the margin.
        /// </summary>
        private const int SEARCH_COLUMN_WIDTH_MINUS_MARGIN = 280;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchWindow"/> class.
        /// </summary>
        public SearchWindow(Common.CouchDB.Database couchdb)
        {
            InitializeComponent();
            _couchdb = couchdb;
            _searchForm = null;
            _boundProperties = new Dictionary<string, UIElement>();
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Thread download;
            Storyboard sb = (Storyboard)TryFindResource("BeginLoadingSearchOptions");
            sb.Begin();

            download = new Thread(new ThreadStart(DownloadSearchOptions));
            download.Start();
        }

        /// <summary>
        /// Downloads the <see cref="Common.NetworkPackage.SearchForm"/> which contains the collection 
        /// of properties for be displayed on the UI.
        /// </summary>
        private void DownloadSearchOptions()
        {
            Common.Network.Message msg = null;
            UpdateUI actUpdateUI = UpdateUI_DownloadComplete;
            
            try
            {
                msg = new Common.Network.Message(Settings.Instance.ServerIp, Settings.Instance.ServerPort, "_settings", "searchform", Common.Network.OperationType.GET,
                     Common.Network.DataStreamMethod.Memory, null, null, null, null, false, false, true, true, Settings.Instance.NetworkBufferSize,
                     Settings.Instance.NetworkTimeout);
                msg.Send();
            }
            catch (Exception e)
            {
                MainWindow.ErrorManager.AddError(Common.ErrorMessage.ErrorCode.SearchOptionDownloadFailed,
                    "Failed to Download Search Fields",
                    "I failed to download the available fields, please try again.",
                    "Failed to download the search fields to be displayed in the SearchWindow.",
                    true, true, e);
                return;
            }

            _searchForm = new Common.NetworkPackage.SearchForm();

            try
            {
                _searchForm.Deserialize(msg.State.Stream);
            }
            catch (Exception e)
            {
                MainWindow.ErrorManager.AddError(Common.ErrorMessage.ErrorCode.SearchOptionDeserializationFailed,
                    "Failed to Read Search Fields",
                    "I failed to read the available searchable fields, please try again.",
                    "Failed to deserialize the search fields to be displayed in the SearchWindow.",
                    true, true, e);
                return;
            }

            Dispatcher.BeginInvoke(actUpdateUI, System.Windows.Threading.DispatcherPriority.Normal);
        }

        /// <summary>
        /// Called when the <see cref="Common.NetworkPackage.SearchForm"/> download is complete.
        /// </summary>
        private void UpdateUI_DownloadComplete()
        {
            Storyboard sbBegin = (Storyboard)TryFindResource("BeginLoadingSearchOptions");
            Storyboard sbEnd = (Storyboard)TryFindResource("EndLoadingSearchOptions");

            if (sbBegin.GetCurrentState() != ClockState.Stopped)
                sbBegin.Stop();

            sbEnd.Begin();
            LoadForm();
        }

        /// <summary>
        /// Constructs the UI.
        /// </summary>
        private void LoadForm()
        {
            Label label = new Label();
            Button btnSearch = new Button();

            label.Content = "Search Criteria";
            label.Margin = new Thickness(10, 5, 10, 0);
            label.Width = SEARCH_COLUMN_WIDTH_MINUS_MARGIN;
            label.FontSize = 12;
            label.FontWeight = FontWeights.Bold;
            label.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            ControlPanel.Children.Add(label);

            for(int i=0; i<_searchForm.Count; i++)
            {
                switch(_searchForm[i].DataType)
                {
                    case Common.NetworkPackage.FormProperty.SupportedDataType.Text:
                        LoadText(_searchForm[i]);
                        break;
                    case Common.NetworkPackage.FormProperty.SupportedDataType.Date:
                        LoadDateRange(_searchForm[i]);
                        break;
                    case Common.NetworkPackage.FormProperty.SupportedDataType.TextCollection:
                        LoadTextCollection(_searchForm[i]);
                        break;
                    default:
                        throw new Exception("Unsupported control type.");
                }
            }

            btnSearch.Content = "Search";
            btnSearch.Margin = new Thickness(10, 10, 10, 0);
            btnSearch.Width = SEARCH_COLUMN_WIDTH_MINUS_MARGIN;
            btnSearch.FontSize = 12;
            btnSearch.FontWeight = FontWeights.Bold;
            btnSearch.Click += new RoutedEventHandler(btnSearch_Click);
            ControlPanel.Children.Add(btnSearch);
        }

        /// <summary>
        /// Handles the Click event of the btnSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Common.Network.Message msg;
            
            Dictionary<string, UIElement>.Enumerator en = _boundProperties.GetEnumerator();
            Common.CouchDB.Lucene.QueryBuilder qb = new Common.CouchDB.Lucene.QueryBuilder();
            string propertyName;
            DateTime from, to;

            // TODO : this needs cleaned up
            while (en.MoveNext())
            {
                if (en.Current.Value.GetType() == typeof(TextBox))
                {
                    propertyName = (string)((TextBox)en.Current.Value).Tag;
                    if(((TextBox)en.Current.Value).Text.Trim().Length > 0)
                        qb.AddText(propertyName, ((TextBox)en.Current.Value).Text.Trim());
                }
                else if (en.Current.Value.GetType() == typeof(DatePicker))
                {
                    propertyName = (string)((DatePicker)en.Current.Value).Tag;
                    propertyName = propertyName.Replace("_from","");

                    if (((DatePicker)en.Current.Value).SelectedDate.HasValue)
                    {
                        from = ((DatePicker)en.Current.Value).SelectedDate.Value;

                        while (en.MoveNext())
                        {
                            if (en.Current.Value.GetType() != typeof(DatePicker))
                                throw new InvalidOperationException("A UIElement of type DatePicker was expected.");
                            else
                                break;
                        }

                        if (!((DatePicker)en.Current.Value).SelectedDate.HasValue)
                            to = DateTime.MaxValue;
                        else
                            to = ((DatePicker)en.Current.Value).SelectedDate.Value;

                        qb.AddDate(propertyName, from, to);
                    }
                    else
                    {
                        from = DateTime.MinValue;

                        while (en.MoveNext())
                        {
                            if (en.Current.Value.GetType() != typeof(DatePicker))
                                throw new InvalidOperationException("A UIElement of type DatePicker was expected.");
                            else
                                break;
                        }

                        // Does to have a selected value?
                        if (((DatePicker)en.Current.Value).SelectedDate.HasValue)
                        {
                            to = DateTime.MaxValue;
                            qb.AddDate(propertyName, from, to);
                        }
                    }
                }
                else
                    throw new Exception("Unsupported UIElement");
            }
            
            Common.CouchDB.Lucene.Search search = new Common.CouchDB.Lucene.Search(_couchdb.Server, _couchdb, "search", "by_all", qb);
            Common.CouchDB.Result result = search.Get<Common.CouchDB.Lucene.SearchResultCollection>(
                60000, 60000, 
                Settings.Instance.NetworkBufferSize, Settings.Instance.NetworkBufferSize);

            List<Guid> versionGuids = new List<Guid>();
            List<Common.Postgres.Version> versions = new List<Common.Postgres.Version>();
            Common.CouchDB.Lucene.SearchResultCollection newSRC = new Common.CouchDB.Lucene.SearchResultCollection();
            DisplayResultsDelegate actDisplayResults = DisplayResults;

            // Get list of all version guids returned
            for (int i = 0; i < search.SearchResultCollection.Count; i++)
                versionGuids.Add(Guid.Parse(search.SearchResultCollection[i].Id));

            // Get all current versions for resources
            versions = Common.Postgres.Version.GetCurrentVersionsFromVersionGuids(versionGuids.ToArray());

            for (int i = 0; i < versions.Count; i++)
            {
                newSRC.Add(search.SearchResultCollection.Get(versions[i].VersionGuid));
            }

            Dispatcher.BeginInvoke(actDisplayResults, dgResults, newSRC);
        }

        /// <summary>
        /// Displays the results on the grid.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="result">The result.</param>
        public void DisplayResults(DataGrid grid, Common.CouchDB.Lucene.SearchResultCollection results)
        {
            grid.Items.Clear();

            for (int i = 0; i < results.Count; i++)
            {
                grid.Items.Add(new SearchWindowDataItem()
                {
                    Guid = results[i].Id,
                    Score = results[i].Score.ToString(),
                    Title = results[i].Fields["title"].ToString(),
                    Extension = results[i].Fields["extension"].ToString(),
                    Creator = results[i].Fields["creator"].ToString(),
                });
            }
        }

        /// <summary>
        /// Creates controls supporting textual input and adds it to the control panel container.
        /// </summary>
        /// <param name="fi">The <see cref="Common.NetworkPackage.FormProperty"/>.</param>
        private void LoadText(Common.NetworkPackage.FormProperty fi)
        {
            Label label = new Label();
            TextBox text = new TextBox();

            label.Content = fi.Label + ":";
            label.Margin = new Thickness(10, 0, 10, 0);
            label.Width = SEARCH_COLUMN_WIDTH_MINUS_MARGIN;

            text.Text = fi.DefaultValue;
            text.Tag = fi.PropertyName;
            text.Margin = new Thickness(10, 0, 10, 0);
            text.Width = SEARCH_COLUMN_WIDTH_MINUS_MARGIN;

            _boundProperties.Add(fi.PropertyName, text);
            ControlPanel.Children.Add(label);
            ControlPanel.Children.Add(text);
        }

        /// <summary>
        /// Creates controls supporting input of multiple textual items and adds it to the control panel container.
        /// </summary>
        /// <param name="fi">The fi.</param>
        private void LoadTextCollection(Common.NetworkPackage.FormProperty fi)
        {
            Label label = new Label();
            TextBox text = new TextBox();

            label.Content = fi.Label + " (use spaces to seperate):";
            label.Margin = new Thickness(10, 0, 10, 0);
            label.Width = SEARCH_COLUMN_WIDTH_MINUS_MARGIN;

            text.Text = fi.DefaultValue;
            text.Tag = fi.PropertyName;
            text.Margin = new Thickness(10, 0, 10, 0);
            text.Width = SEARCH_COLUMN_WIDTH_MINUS_MARGIN;

            _boundProperties.Add(fi.PropertyName, text);
            ControlPanel.Children.Add(label);
            ControlPanel.Children.Add(text);
        }

        /// <summary>
        /// Creates controls supporting temporial input and adds it to the control panel container.
        /// </summary>
        /// <param name="fi">The fi.</param>
        private void LoadDateRange(Common.NetworkPackage.FormProperty fi)
        {
            Label label = new Label();
            Label lblFrom = new Label();
            Label lblTo = new Label();
            DatePicker from = new DatePicker();
            DatePicker to = new DatePicker();
            StackPanel sp = new StackPanel();

            // Primary Label
            label.Content = fi.Label + ":";
            label.Margin = new Thickness(10, 0, 10, 0);
            label.Width = SEARCH_COLUMN_WIDTH_MINUS_MARGIN;

            // Picker Ranger - Panel
            sp.Margin = new Thickness(10, 0, 10, 0);
            sp.Width = SEARCH_COLUMN_WIDTH_MINUS_MARGIN;
            sp.Orientation = Orientation.Horizontal;
            
            // From
            lblFrom.Content = "From:";
            lblFrom.Margin = new Thickness(10, 0, 0, 0);
            from.Margin = new Thickness(0, 0, 0, 0);
            from.Tag = fi.PropertyName + "_start";
            _boundProperties.Add(fi.PropertyName + "_start", from);

            // To
            lblTo.Content = "To:";
            lblTo.Margin = new Thickness(0, 0, 0, 0);
            to.Margin = new Thickness(0, 0, 0, 0);
            to.Tag = fi.PropertyName + "_stop";
            _boundProperties.Add(fi.PropertyName + "_stop", to);

            sp.Children.Add(lblFrom);
            sp.Children.Add(from);
            sp.Children.Add(lblTo);
            sp.Children.Add(to);

            ControlPanel.Children.Add(label);
            ControlPanel.Children.Add(sp);
        }

        /// <summary>
        /// Handles the Click event of the Button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SearchWindowDataItem item = (SearchWindowDataItem)((FrameworkElement)sender).DataContext;
            if (OnResultSelected != null) OnResultSelected(new Guid(item.Guid));
            Close();
        }
    }
}
