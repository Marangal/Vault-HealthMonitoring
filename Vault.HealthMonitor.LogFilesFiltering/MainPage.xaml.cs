using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Vault.HealthMonitor.LogFilesFiltering
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string ConfigurationFolderToken = "ConfigurationFolderToken";
        private const string DestinationFolderToken = "DestinationFolderToken";
        private const string FilterConfigurationFileName = "FilterConfiguration.config";
        private StorageFolder ConfigurationFolder { get; set; }
        public MainPage()
        {
            this.InitializeComponent();
            tbAppVersion.Text += GetAppVersion();
            tbAssemblyFileVersion.Text += GetAssemblyFileVersion();
        }

        private string GetAppVersion()
        {

            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private string GetAssemblyFileVersion()
        {
            
            var assembly = typeof(MainPage).GetTypeInfo().Assembly;
            var assemblyVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            return assemblyVersion;
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckPermissions();
        }

        private async Task CheckPermissions()
        {
            try
            {
                bool containsConfigurationToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.ContainsItem(ConfigurationFolderToken);
                bool containsConfigFile = false;
                if (containsConfigurationToken)
                {
                    try
                    {
                        ConfigurationFolder = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(ConfigurationFolderToken);
                        containsConfigFile = await ContainsConfigurationFile(ConfigurationFolder);
                    }
                    catch (FileNotFoundException)
                    {
                        containsConfigFile = false;
                    }
                }

                if (!containsConfigurationToken || !containsConfigFile)
                {
                    btnEditConfiguration.IsEnabled = false;
                    btnFilterLogFiles.IsEnabled = false;
                }
                else
                {
                    tbSettingsLocation.Text = ConfigurationFolder.Path;
                }
            } catch(Exception ex)
            {
                await ShowError(ex);
            }
            
        }

        private async void btnFilterLogFiles_Click(object sender, RoutedEventArgs e)
        {

            btnFilterLogFiles.IsEnabled = false;
            object intitialContent = btnFilterLogFiles.Content;
            btnFilterLogFiles.Content = "Selecting..";

            await FilterLogFiles();

            btnFilterLogFiles.IsEnabled = true;
            btnFilterLogFiles.Content = intitialContent;
        }

        private async Task FilterLogFiles()
        {
            try
            {
                btnFilterLogFiles.Content = "Reading Configuration..";
                Configuration.FilterConfiguration filterConfiguration = await Configuration.ConfigSerialization.Deserialize<Configuration.FilterConfiguration>(Path.Combine(ConfigurationFolder.Path, FilterConfigurationFileName));
                string errorMessage;
                if (!filterConfiguration.Validate(out errorMessage))
                {
                    var errorDialog = new MessageDialog($"Error={errorMessage}", $"Invalid configuration file.");
                    errorDialog.Commands.Add(new UICommand { Label = "Close", Id = 0 });
                    await errorDialog.ShowAsync();
                    return;
                }

                IReadOnlyList<StorageFile> files = await OpenFilePickerAsync();
                if (files == null)
                    return;
                

                btnFilterLogFiles.Content = "Reading..";
                Domain.FileToFilter[] filesToFilter = await BL.FileToFilterFactory.CreateFilesToFilterAsync(files, filterConfiguration);
                if(filesToFilter.Length == 0)
                {
                    var errorDialog = new MessageDialog($"Please change your configuration file or select other files.", $"0 supported files selected");
                    errorDialog.Commands.Add(new UICommand { Label = "Close", Id = 0 });
                    await errorDialog.ShowAsync();
                    return;
                }


                StorageFolder destinationFolder = await SelectDestinationFolder();
                if (destinationFolder == null)
                    return;

                SaveFolderToFutureAccessList(destinationFolder, DestinationFolderToken);
                List<Task> tasks = new List<Task>();
                foreach (var fileToFilter in filesToFilter)
                {
                    IEnumerable<Domain.LineGroup> lineGroups = fileToFilter.GetImportantLineGroups();
                    tasks.Add(CreateNewFilteredFile(destinationFolder, fileToFilter, lineGroups));
                }
                await Task.WhenAll(tasks);

                var completedDialog = new MessageDialog($"Output folder:{destinationFolder.Path}", $"Completed filtering {tasks.Count} log file(s)");
                completedDialog.Commands.Add(new UICommand { Label = "Done", Id = 0 });
                await completedDialog.ShowAsync();

                await Launcher.LaunchFolderAsync(destinationFolder);
            }
            catch (Exception ex)
            {
                await ShowError(ex);
            }
        }

        private async Task<StorageFolder> SelectDestinationFolder()
        {
            var dialog = new MessageDialog($"", $"Select folder to drop filtered files");
            dialog.Commands.Add(new UICommand { Label = "Select destination folder", Id = 0 });
            dialog.Commands.Add(new UICommand { Label = $"Cancel", Id = 1 });
            var res = await dialog.ShowAsync();

            StorageFolder destinationFolder = null;
            switch ((int)res.Id)
            {
                case 0:
                    destinationFolder = await TrySelectFolder();
                    break;
                case 1:
                    break;
            }
            return destinationFolder;
        }

        private async Task CreateNewFilteredFile(StorageFolder destinationFolder, Domain.FileToFilter fileToFilter, IEnumerable<Domain.LineGroup> lineGroups)
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileToFilter.File.Name);
            string extension = Path.GetExtension(fileToFilter.File.Name);

            StringBuilder sb = new StringBuilder();
            foreach (var lineGroup in lineGroups)
                sb.Append(lineGroup.ToString());

            string newFileName = $"{nameWithoutExtension}-filtered{extension}";
            StorageFile file = await destinationFolder.CreateFileAsync(newFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, sb.ToString());
        }

        public async Task<IReadOnlyList<StorageFile>> OpenFilePickerAsync()
        {
            var dialog = new MessageDialog($"Select the files you want to filter", $"Select files to filter");
            dialog.Commands.Add(new UICommand { Label = "Select files", Id = 0 });
            dialog.Commands.Add(new UICommand { Label = $"Cancel", Id = 1 });
            var res = await dialog.ShowAsync();

            IReadOnlyList<StorageFile> files = null;
            switch ((int)res.Id)
            {
                case 0:
                    FileOpenPicker openPicker = new FileOpenPicker();
                    openPicker.ViewMode = PickerViewMode.List;
                    openPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                    openPicker.FileTypeFilter.Add(".txt");
                    files = await openPicker.PickMultipleFilesAsync();
                    break;
                case 1:                    
                    break;
            }
            return files;
        }

        private async void btnSelectNetworkConfigurationFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StorageFolder folder = await TrySelectFolder();
                if (folder != null)
                {
                    bool containsFile = await ContainsConfigurationFile(folder);
                    if (!containsFile)
                    {
                        var dialog = new MessageDialog($"{FilterConfigurationFileName} is not found in the selected folder {folder.Path}", $"Missing {FilterConfigurationFileName}");
                        dialog.Commands.Add(new UICommand { Label = "Select other folder", Id = 0 });
                        dialog.Commands.Add(new UICommand { Label = $"Create new {FilterConfigurationFileName}", Id = 1 });
                        dialog.Commands.Add(new UICommand { Label = $"Cancel", Id = 2 });
                        var res = await dialog.ShowAsync();

                        switch ((int)res.Id)
                        {
                            case 0:
                                btnSelectNetworkConfigurationFolder_Click(sender, e);
                                break;
                            case 1:
                                SaveFolderToFutureAccessList(folder, ConfigurationFolderToken);
                                tbSettingsLocation.Text = folder.Path;
                                await CreateNewFilterConfigurationFile(folder);
                                break;
                            case 2:
                                break;
                        }
                    }
                    else
                    {
                        SaveFolderToFutureAccessList(folder, ConfigurationFolderToken);

                        tbSettingsLocation.Text = folder.Path;
                        ConfigurationFolder = folder;
                    }
                }
            }
            catch(Exception ex)
            {
                await ShowError(ex);
            }
        }

        private async Task ShowError(Exception ex)
        {
            var dialog = new MessageDialog($"ERROR {ex.ToString()}", $"{ex.Message}");
            dialog.Commands.Add(new UICommand { Label = "Close", Id = 0 });
            var res = await dialog.ShowAsync();
        }

        private async Task CreateNewFilterConfigurationFile(StorageFolder folder)
        {
            Configuration.FilterConfiguration filterConfig = new Configuration.FilterConfiguration();
            filterConfig.FileTypeConfigurations.Add(new Configuration.FileTypeConfiguration()
            {
                FileNamePrefix = "vlog",
                StartLineGroups = new List<string>()
                {
                    "d-M-yyyy H:mm:ss",
                    "d/M/yyyy H:mm:ss",
                    "d.M.yyyy H:mm:ss"
                },
                KnownLogErrors = new List<Configuration.KnowLogError>()
                {
                    new Configuration.KnowLogError()
                    {
                        ContainsValue = "FullContentSearchContentIndexingDisabled [1106]",
                        DangerWhenMoreThenXOccurences = 10
                    }
                }
            });
            await Configuration.ConfigSerialization.Serialize<Configuration.FilterConfiguration>(Path.Combine(folder.Path, FilterConfigurationFileName), filterConfig);
            ConfigurationFolder = folder;
        }

        private async Task<StorageFolder> TrySelectFolder()
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.ViewMode = PickerViewMode.List;            
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            return folder;
        }

        private async Task<bool> ContainsConfigurationFile(StorageFolder folder)
        {
            try
            {
                IStorageItem filterConfigurationFile = await folder.TryGetItemAsync(FilterConfigurationFileName);
                if (filterConfigurationFile != null)
                {
                    return true;
                }
            }
            catch(Exception)
            {
                return false;
            }
            return false;
        }

        public void SaveFolderToFutureAccessList(StorageFolder folder, string token)
        {
            // Application now has read/write access to all contents in the picked folder
            // (including other sub-folder contents)
            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, folder);

            btnEditConfiguration.IsEnabled = true;
            btnFilterLogFiles.IsEnabled = true;
        }

        private async void btnEditConfiguration_Click(object sender, RoutedEventArgs e)
        {
            string configurationFilePath = Path.Combine(ConfigurationFolder.Path, FilterConfigurationFileName);
            StorageFile file = await StorageFile.GetFileFromPathAsync(configurationFilePath);
            await Launcher.LaunchFileAsync(file);
        }
    }
}
