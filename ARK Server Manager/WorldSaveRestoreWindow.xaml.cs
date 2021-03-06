﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ARK_Server_Manager.Lib;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for WorldSaveRestoreWindow.xaml
    /// </summary>
    public partial class WorldSaveRestoreWindow : Window
    {
        public class WorldSaveFileList : SortableObservableCollection<WorldSaveFile>  
        {
            public new void Add(WorldSaveFile item)
            {
                if (item == null || this.Any(m => m.FileName.Equals(item.FileName)))
                    return;

                base.Add(item);
            }

            public override string ToString()
            {
                return $"{nameof(WorldSaveFile)} - {Count}";
            }
        }

        public class WorldSaveFile : DependencyObject 
        {
            public static readonly DependencyProperty CreatedDateProperty = DependencyProperty.Register(nameof(CreatedDate), typeof(DateTime), typeof(WorldSaveFile), new PropertyMetadata(DateTime.MinValue));
            public static readonly DependencyProperty FileProperty = DependencyProperty.Register(nameof(File), typeof(string), typeof(WorldSaveFile), new PropertyMetadata(string.Empty));
            public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(nameof(FileName), typeof(string), typeof(WorldSaveFile), new PropertyMetadata(string.Empty));
            public static readonly DependencyProperty UpdatedDateProperty = DependencyProperty.Register(nameof(UpdatedDate), typeof(DateTime), typeof(WorldSaveFile), new PropertyMetadata(DateTime.MinValue));
            public static readonly DependencyProperty IsActiveFileProperty = DependencyProperty.Register(nameof(IsActiveFile), typeof(bool), typeof(WorldSaveFile), new PropertyMetadata(false));

            public DateTime CreatedDate
            {
                get { return (DateTime)GetValue(CreatedDateProperty); }
                set { SetValue(CreatedDateProperty, value); }
            }

            public string File
            {
                get { return (string)GetValue(FileProperty); }
                set { SetValue(FileProperty, value); }
            }

            public string FileName
            {
                get { return (string)GetValue(FileNameProperty); }
                set { SetValue(FileNameProperty, value); }
            }

            public DateTime UpdatedDate
            {
                get { return (DateTime)GetValue(UpdatedDateProperty); }
                set { SetValue(UpdatedDateProperty, value); }
            }

            public bool IsActiveFile
            {
                get { return (bool)GetValue(IsActiveFileProperty); }
                set { SetValue(IsActiveFileProperty, value); }
            }

            public override string ToString()
            {
                return FileName;
            }
        }

        public class WorldSaveFileComparer : IComparer<WorldSaveFile>
        {
            public int Compare(WorldSaveFile x, WorldSaveFile y)
            {
                if (x == null && y == null)
                    return 0;
                if (x == null)
                    return 1;
                if (y == null)
                    return -1;

                if (x.IsActiveFile && y.IsActiveFile)
                {
                    if (x.UpdatedDate == y.UpdatedDate)
                        return 0;
                    return x.UpdatedDate < y.UpdatedDate ? 1 : -1;
                }
                if (x.IsActiveFile)
                    return -1;
                if (y.IsActiveFile)
                    return 1;

                if (x.UpdatedDate == y.UpdatedDate)
                    return 0;
                return x.UpdatedDate < y.UpdatedDate ? 1 : -1;
            }
        }

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private readonly ServerProfile _profile = null;

        public static readonly DependencyProperty WorldSaveFilesProperty = DependencyProperty.Register(nameof(WorldSaveFiles), typeof(WorldSaveFileList), typeof(WorldSaveRestoreWindow), new PropertyMetadata(null));

        public WorldSaveRestoreWindow(ServerProfile profile)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            _profile = profile;
            this.Title = string.Format(_globalizer.GetResourceString("WorldSaveRestore_ProfileTitle"), _profile?.ProfileName);

            WorldSaveFiles = new WorldSaveFileList();

            this.DataContext = this;
        }

        public WorldSaveFileList WorldSaveFiles
        {
            get { return GetValue(WorldSaveFilesProperty) as WorldSaveFileList; }
            set { SetValue(WorldSaveFilesProperty, value); }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadWorldSaveFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("WorldSaveRestore_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var item = ((WorldSaveFile)((Button)e.Source).DataContext);

            var message = $"You are able to delete worldsave file {item.FileName}.\r\n\r\nDo you want to continue?";
            if (MessageBox.Show(this, message, "Delete WorldSave Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                File.Delete(item.File);

                MessageBox.Show(this, "The worldsave file has been deleted.", "Delete WorldSave Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Delete WorldSave Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await LoadWorldSaveFiles();
            }
        }

        private async void Reload_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                await LoadWorldSaveFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("WorldSaveRestore_Refresh_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            var item = ((WorldSaveFile)((Button)e.Source).DataContext);
            
            var message = $"You are about to restore worldsave file {item.FileName}.\r\n\r\nDo you want to continue?";
            if (MessageBox.Show(this, message, "Restore WorldSave Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _profile.RestoreWorldSave(item.File);

                MessageBox.Show(this, "The worldsave file has been restored.", "Restore WorldSave Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Restore WorldSave Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await LoadWorldSaveFiles();
            }
        }

        private async Task LoadWorldSaveFiles()
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                WorldSaveFiles.Clear();

                var profileSaveFolder = ServerProfile.GetProfileSavePath(_profile);
                if (!Directory.Exists(profileSaveFolder))
                    return;

                var profileSaveFolderInfo = new DirectoryInfo(profileSaveFolder);
                var mapName = ServerProfile.GetProfileMapFileName(_profile);
                var mapFileName = $"{mapName}.ark";
                var searchPattern = $"{mapName}*.ark";

                foreach (var file in profileSaveFolderInfo.GetFiles(searchPattern))
                {
                    WorldSaveFiles.Add(new WorldSaveFile { File = file.FullName , FileName = file.Name, CreatedDate = file.CreationTime, UpdatedDate = file.LastWriteTime, IsActiveFile = file.Name.Equals(mapFileName, StringComparison.OrdinalIgnoreCase) });
                }

                WorldSaveFiles.Sort(f => f, new WorldSaveFileComparer());
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }
    }
}
