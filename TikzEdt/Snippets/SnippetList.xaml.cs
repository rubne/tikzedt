﻿/*This file is part of TikzEdt.
 
TikzEdt is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
 
TikzEdt is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.
 
You should have received a copy of the GNU General Public License
along with TikzEdt.  If not, see <http://www.gnu.org/licenses/>.*/

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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Data;


namespace TikzEdt.Snippets
{
    /// <summary>
    /// Interaction logic for SnippetList.xaml
    /// </summary>
    public partial class SnippetList : UserControl
    {        
        public delegate void InsertEventHandler(string code, string dependencies);
        public event InsertEventHandler OnInsert;

        public class UseStylesEventArgs : EventArgs
        {
            public string nodestyle="", edgestyle="", dependencies="";
            /// <summary>
            /// Indicates whether the style should be used in addition to the ones present
            /// </summary>
            public bool InAddition = false;
        }        
        public event EventHandler<UseStylesEventArgs> OnUseStyles;

        readonly public static DependencyProperty ShowThumbnailsProperty = DependencyProperty.Register(
         "ShowThumbnails", typeof(bool), typeof(SnippetList), new PropertyMetadata(true));
        public bool ShowThumbnails
        {
            get { return (bool)GetValue(ShowThumbnailsProperty); }
            set { SetValue(ShowThumbnailsProperty, value); }
        }



        public double ThumbnailSize
        {
            get { return (double)GetValue(ThumbnailSizeProperty); }
            set { SetValue(ThumbnailSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ThumbnailSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ThumbnailSizeProperty =
            DependencyProperty.Register("ThumbnailSize", typeof(double), typeof(SnippetList), new UIPropertyMetadata(40.0));

        


        public SnippetList()
        {
            InitializeComponent();

            // there is a binding problem (bug) that makes the following line necessary
            NameScope.SetNameScope(contextMenu, NameScope.GetNameScope(this));

        }

        SnippetsDataSet snippetsDataSet;
        CollectionViewSource snippetsTableViewSource;
        SnippetsDataSet.SnippetsTableDataTable snippetsTable;
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {            
            snippetsDataSet = ((SnippetsDataSet)(this.FindResource("snippetsDataSet")));
            snippetsTable = snippetsDataSet.Tables["SnippetsTable"] as SnippetsDataSet.SnippetsTableDataTable;
            snippetsTableViewSource = (CollectionViewSource)this.FindResource("snippetsTableViewSource");

            Reload();

            // Do Thumbnails exist? -> Unzip or Recompile
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))  // we don't want this to happen in the vs designer
            {
                if (!Directory.Exists(Helper.GetSnippetsPath()))
                {
                    // first try to unzip snippets
                    string zip = System.IO.Path.Combine( Helper.GetAppDir(), Consts.cSnippetThumbsZipfile);
                    string unzipper = System.IO.Path.Combine( Helper.GetAppDir(), Consts.cUnzipper);
                    if (File.Exists(zip) && File.Exists(unzipper))
                    {
                        try
                        {
                            GlobalUI.UI.AddStatusLine(this, "Unzipping snippet thumbnails from file " + zip + "....");
                            System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo()
                                {
                                    UseShellExecute = false,
                                    FileName = unzipper,
                                    Arguments = "\""+zip+"\" \""+Helper.GetAppdataPath()+"\"",
                                    CreateNoWindow = true
                                });
                        }
                        catch (Exception ex)
                        {
                            GlobalUI.UI.AddStatusLine(this, "Unzipping snippet thumbnails failed: " + ex.Message, true);
                            CompileSnippets();  // in case of failure, try to recompile snippets
                        }
                    }
                    else
                        CompileSnippets();
                }
            }
        }

        public void CompileSnippets()
        {
            if (GlobalUI.UI.ShowMessageBox("Do you want to create the Snippet Thumbnails now?\r\n" +
    "It may take some time, but it will happen in the background. You can also recompile them later from the menu or the Snippet Manager." +
    "Note: If you are missing some Latex packages, it is better to compile later", "Compile Thumbnails",
    System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                // Compile
                foreach (SnippetsDataSet.SnippetsTableRow r in snippetsTable.Rows)
                {
                    if (!r.IsNull(snippetsTable.SampleCodeColumn))
                    {
                        string cFile = Helper.GetSnippetsPath() + r.ID;
                        TikzToBMPFactory.Instance.AddJob(r.SampleCode, cFile + ".tex", new Rect(0, 0, 0, 0), r.Name, true);
                    }
                }
            }
        }

        public void Reload()
        {
            if (File.Exists(Helper.GetSettingsPath() + Consts.cSnippetsFile))
            {
                snippetsDataSet.Clear();
                snippetsDataSet.ReadXml(Helper.GetSettingsPath() + Consts.cSnippetsFile);
                snippetsTableViewSource.View.Refresh();
                //snippetsTableViewSource.SortDescriptions.Clear();
                //snippetsTableViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("Category", System.ComponentModel.ListSortDirection.Ascending));
                //snippetsTableViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
                //lstSnippets.ItemsSource = from rows in snippetsTable
                //                          orderby rows.Name
                //                          group rows by rows.Category into g
                //                          select g;
                                          //group rows by rows.Category into g
                                          //orderby g.Key
                                          //select g;
            }
        }

        private void lstSnippets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstSnippets.SelectedItem != null)
            {
                string c="", d="";
                SnippetsDataSet.SnippetsTableRow r = ((DataRowView)lstSnippets.SelectedItem).Row as SnippetsDataSet.SnippetsTableRow;
                if (!r.IsNull(snippetsTable.SnippetCodeColumn))
                    c = r.SnippetCode;
                if (!r.IsNull(snippetsTable.DependenciesColumn))
                    d = r.Dependencies;

                if (OnInsert!= null)
                    OnInsert(c,d);
            }
        }

        private void InsertSnippetClick(object sender, RoutedEventArgs e)
        {
            if (lstSnippets.SelectedItem != null)
            {
                string c="", d="";
                SnippetsDataSet.SnippetsTableRow r = ((DataRowView)lstSnippets.SelectedItem).Row as SnippetsDataSet.SnippetsTableRow;
                if (!r.IsNull(snippetsTable.SnippetCodeColumn))
                    c = r.SnippetCode;
                if (!r.IsNull(snippetsTable.DependenciesColumn))
                    d = r.Dependencies;

                if (OnInsert!= null)
                    OnInsert(c,d);
            }
        }

        private void InsertFullCodeClick(object sender, RoutedEventArgs e)
        {
            if (lstSnippets.SelectedItem != null)
            {
                string c = "", d = "";
                SnippetsDataSet.SnippetsTableRow r = ((DataRowView)lstSnippets.SelectedItem).Row as SnippetsDataSet.SnippetsTableRow;
                if (!r.IsNull(snippetsTable.SampleCodeColumn))
                    c = r.SampleCode;
                if (!r.IsNull(snippetsTable.DependenciesColumn))
                    d = r.Dependencies;

                if (OnInsert != null)
                    OnInsert(c, d);
            }
        }

        private void InsertDependenciesClick(object sender, RoutedEventArgs e)
        {
            if (lstSnippets.SelectedItem != null)
            {
                string d = "";
                SnippetsDataSet.SnippetsTableRow r = ((DataRowView)lstSnippets.SelectedItem).Row as SnippetsDataSet.SnippetsTableRow;
                if (!r.IsNull(snippetsTable.DependenciesColumn))
                    d = r.Dependencies;

                if (OnInsert != null && d.Trim()!="")
                    OnInsert(@"\usetikzlibrary{"+d+"}"+Environment.NewLine, d);
            }
        }

        private void InsertAsTikzStyle_Click(object sender, RoutedEventArgs e)
        {
            // find row corresponding to clicked button
            object curItem = ((ListBoxItem)lstSnippets.ContainerFromElement((Button)sender)).Content;
            if (curItem != null)
            {
                string toinsert = "", dependencies="";
                SnippetsDataSet.SnippetsTableRow r = (curItem as DataRowView).Row as SnippetsDataSet.SnippetsTableRow;
                if (!r.IsNull(snippetsTable.NodeStyleColumn) && ! (r.NodeStyle.Trim()=="") )
                    toinsert += "\\tikzstyle{mynodestyle} = ["+ r.NodeStyle +"]"+Environment.NewLine;
                if (!r.IsNull(snippetsTable.EdgeStyleColumn) && !(r.EdgeStyle.Trim() == ""))
                    toinsert += "\\tikzstyle{myedgestyle} = [" + r.EdgeStyle + "]" + Environment.NewLine;
                if (!r.IsNull(snippetsTable.DependenciesColumn))
                    dependencies = r.Dependencies;
                
                if (OnInsert != null)
                    OnInsert(toinsert, dependencies);
            }
            
        }

        private void UseStyleButton_Click(object sender, RoutedEventArgs e)
        {
            // find row corresponding to clicked button
            object curItem = ((ListBoxItem)lstSnippets.ContainerFromElement((Button)sender)).Content;
            if (curItem != null)
            {
                UseStylesEventArgs args = new UseStylesEventArgs();
                SnippetsDataSet.SnippetsTableRow r = (curItem as DataRowView).Row as SnippetsDataSet.SnippetsTableRow;
                if (!r.IsNull(snippetsTable.NodeStyleColumn))
                    args.nodestyle = r.NodeStyle;
                if (!r.IsNull(snippetsTable.EdgeStyleColumn))
                    args.edgestyle = r.EdgeStyle;
                if (!r.IsNull(snippetsTable.DependenciesColumn))
                    args.dependencies = r.Dependencies;
                args.InAddition = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

                if (OnUseStyles != null)
                    OnUseStyles(this, args);
            }
        }
    }

    /// <summary>
    /// Returns visible iff one of the strings is nonempty (and non-null)
    /// </summary>
    public class StringToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
                              object parameter, System.Globalization.CultureInfo culture)
        {
            if ((values[0] is string && values[0].ToString().Trim() != "") || (values[1] is string && values[1].ToString().Trim() != ""))
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetType,
                                  object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
