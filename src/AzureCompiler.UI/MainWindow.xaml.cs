using AzureCompiler.Core;
using AzureCompiler.UI.Properties;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AzureCompiler.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<AcumaticaVersion> AcumaticaVersions { get; set; }
        public AcumaticaVersion SelectedAcumatica { get; set; }

        public ObservableCollection<string> VMSizes { get; set; }
        public string SelectedSize { get; set; }

        private string _customConfig { get; set; }

        private const string _logFile = "log.txt";

        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists(_logFile))
                File.Delete(_logFile);

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(_logFile)
            .CreateLogger();
            DataContext = this;
            AcumaticaVersions = new ObservableCollection<AcumaticaVersion>
            {
                new AcumaticaVersion { FrameworkId = "NDP48", FrameworkName = ".NET 4.8", AcumaticaRelease = "2020R1/2019R2" },
                new AcumaticaVersion { FrameworkId = "NDP472", FrameworkName = ".NET 4.7.2", AcumaticaRelease = "2019R1" },
            };
            VMSizes = VMSizesList.GetSizes();
            if (string.IsNullOrEmpty(Settings.Default.AcuVersion))
                SelectedAcumatica = AcumaticaVersions.FirstOrDefault(x => x.FrameworkId == "NDP48");
            else
                SelectedAcumatica = AcumaticaVersions.FirstOrDefault(x => x.AcumaticaRelease == Settings.Default.AcuVersion);           

            TbSDK.Text = Settings.Default.SDK;
            TbSource.Text = Settings.Default.PathToSource;
            TbOutput.Text = Settings.Default.PathToOutput;
            RbUseCustom.IsChecked = Settings.Default.CustomConfigRB;
            if (!string.IsNullOrEmpty(Settings.Default.CustomConfigPath))
                TbConfig.Text = Settings.Default.CustomConfigPath;

            if (!string.IsNullOrEmpty(Settings.Default.VMSize))
                SelectedSize = Settings.Default.VMSize;
            else
                SelectedSize = "Medium";
            CbVmSize.Loaded += TargetComboBox_Loaded;
        }

        private void TexBox_Path_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                var result = dialog.ShowDialog();
                if (sender is TextBox tb && result == System.Windows.Forms.DialogResult.OK)
                {            
                    tb.Text = dialog.SelectedPath;
                }
            }
        }     

        private void CbAcumaticaVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TbFramework.Text = SelectedAcumatica.FrameworkName;
        }    

        private void TargetComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var targetComboBox = sender as ComboBox;
            var targetTextBox = targetComboBox?.Template.FindName("PART_EditableTextBox", targetComboBox) as TextBox;

            if (targetTextBox == null) return;

            targetComboBox.Tag = "TextInput";
            targetComboBox.StaysOpenOnEdit = true;
            targetComboBox.IsEditable = true;
            targetComboBox.IsTextSearchEnabled = false;

            targetTextBox.TextChanged += (o, args) =>
            {
                var textBox = (TextBox)o;

                var searchText = textBox.Text;

                if (targetComboBox.Tag.ToString() == "Selection")
                {
                    targetComboBox.Tag = "TextInput";
                    targetComboBox.IsDropDownOpen = true;
                }
                else
                {
                    if (targetComboBox.SelectionBoxItem != null)
                    {
                        targetComboBox.SelectedItem = null;
                        targetTextBox.Text = searchText;
                    }

                    if (string.IsNullOrEmpty(searchText))
                    {
                        targetComboBox.Items.Filter = item => true;
                        targetComboBox.SelectedItem = default(object);
                    }
                    else
                        targetComboBox.Items.Filter = item =>
                                item.ToString().StartsWith(searchText, true, CultureInfo.InvariantCulture);

                    Keyboard.Focus(targetTextBox);
                    targetComboBox.IsDropDownOpen = true;
                }
            };

            targetComboBox.SelectionChanged += (o, args) =>
            {
                var comboBox = o as ComboBox;
                if (comboBox?.SelectedItem == null) return;
                comboBox.Tag = "Selection";
            };
        }

        private void TbConfig_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        { 
            using (var openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "config (*.csdef)|*.csdef";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TbConfig.Text = openFileDialog.FileName;
                    _customConfig = openFileDialog.FileName;
                    CbVmSize.IsEditable = false;
                    CbVmSize.IsEnabled = false;          
                    CbVmSize.SelectedItem = null;                
                }
            }
        }

        private void BtConfig_Click(object sender, RoutedEventArgs e)
        {
            TbConfig_MouseDoubleClick(null, null);
        }

        private void TbConfig_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (tb.Text.Length == 0)
                {
                    CbVmSize.IsEditable = true;
                    CbVmSize.IsEnabled = true;
                    CbVmSize.SelectedItem = SelectedSize;
                }           
            }
        }
      
        private void Button_Path_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                string tbName = "Tb" + bt.Name.Replace("Bt", "");
                TextBox foundTextBox = FindChild<TextBox>(Application.Current.MainWindow, tbName);
                TexBox_Path_MouseDoubleClick(foundTextBox, null);
            }
        }

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }
            return foundChild;
        }

        private void Config_Checked(object sender, RoutedEventArgs e)
        {
            if (RbUseCustom == null)
                return;
            bool standard = !(bool)RbUseCustom.IsChecked;
            bool custom = (bool)RbUseCustom.IsChecked;           
            LbSize.IsEnabled = standard;
            CbVmSize.IsEnabled = standard;
            LbCustom.IsEnabled = custom;
            TbConfig.IsEnabled = custom;
            BtConfig.IsEnabled = custom;            
        }

        private void BtnCompile_Click(object sender, RoutedEventArgs e)
        {
            string config = TbConfig.Text;
            if ((bool)RbUseStandard.IsChecked)
            {
                config = "";
                if (string.IsNullOrEmpty(SelectedSize))
                {
                    if (string.IsNullOrEmpty(CbVmSize.Text))
                        MessageBox.Show("Please fill VM Size", "Azure Compiler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        SelectedSize = CbVmSize.Text;
                }
            }

            Settings.Default.AcuVersion = SelectedAcumatica.AcumaticaRelease;
            Settings.Default.SDK = TbSDK.Text;
            Settings.Default.PathToSource = TbSource.Text;
            Settings.Default.PathToOutput = TbOutput.Text;
            Settings.Default.CustomConfigRB = (bool)RbUseCustom.IsChecked;
            Settings.Default.CustomConfigPath = TbConfig.Text;
            Settings.Default.VMSize = SelectedSize;
            Settings.Default.Save();

            AssemblyAzurePackage assemblyAzure = new AssemblyAzurePackage(TbSDK.Text,TbSource.Text, TbOutput.Text, SelectedAcumatica.FrameworkId, SelectedSize, config);
            int result = assemblyAzure.Build();

            if (result == 0)
                MessageBox.Show("Compilation completed", "Azure Compiler", MessageBoxButton.OK, MessageBoxImage.Information);
            else if (result == 1)
                MessageBox.Show("Compilation finished with error. For more information read file log.txt", "Azure Compiler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
