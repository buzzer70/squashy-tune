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

using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using NAudio.Lame;
using NAudio.Wave;

namespace MusicSquash
{


  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();

      StatusLabel.Content = "";
      mMusicFiles = new List<string>();
      mSettings = MusicSquashSettings.Load();
      mCompressedFolder = "";

      MuiscFolderLabel.Content = "";
      if (mSettings.MusicFolder != "")
      {
        if (Directory.Exists(mSettings.MusicFolder))
        {
          SetFolder();
        }
        else
        {
          mSettings.MusicFolder = "";
          mSettings.Save();
        }

      }

    }

    private DirectoryInfo mRootFolder;
    private List<string> mMusicFiles;
    private MusicSquashSettings mSettings;
    private string mCompressedFolder;


    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new CommonOpenFileDialog();
      dialog.IsFolderPicker = true;

      //check settings save
      if (mSettings.MusicFolder != "")
      {
        if (Directory.Exists(mSettings.MusicFolder))
        {
          dialog.DefaultDirectory = mSettings.MusicFolder;
        }
      }

      CommonFileDialogResult result = dialog.ShowDialog();
      if (result == CommonFileDialogResult.Ok)
      {
        //save folder in settings
        mSettings.MusicFolder = dialog.FileName;
        mSettings.Save();
        SetFolder();
      }
    }

    private void SetFolder()
    {
      MuiscFolderLabel.Content = mSettings.MusicFolder;
      FolderlistBox.Items.Clear();
      //folder selected
      if (Directory.Exists(mSettings.MusicFolder))
      {
        mRootFolder = new DirectoryInfo(mSettings.MusicFolder);

        var justoriginalfolders = mRootFolder.GetDirectories().Where(name => !name.ToString().StartsWith("compressed"));

        foreach (var ms in justoriginalfolders)
        {
          FolderlistBox.Items.Add(ms.Name);
        }
         
      }
      else
      {

      }
    }


    private void CompressButton_Click(object sender, RoutedEventArgs e)
    {
      if (mMusicFiles.Count() > 0)
      {
        if (MessageBox.Show(string.Format("Converting {0} Wav file(s).\nAre You Sure?", mMusicFiles.Count()), "Confirm Compression",
          MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
          ConvertFiles();
        }
      }
    }



    private void FolderlistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (FolderlistBox.SelectedIndex > -1)
      {
        string name = FolderlistBox.SelectedValue.ToString();
        string musicfolder = string.Format(@"{0}\{1}", mRootFolder.FullName, name);
        string[] wavfiles = Directory.GetFiles(musicfolder, "*.wav");
        mMusicFiles.Clear();
          mMusicFiles.AddRange(wavfiles);
  
        StatusLabel.Content = string.Format("{0} Wav file(s) found for conversion.", wavfiles.Count());
        //get info for first file to create compression folder

        mCompressedFolder = string.Format(@"{0}\compressed-{1}", mRootFolder.FullName, name);
        CompressButton.IsEnabled = true;
      }
      else
      {
        CompressButton.IsEnabled = false;
      }

    }

    private void ConvertFiles()
    {

      
      if (Directory.Exists(mCompressedFolder) == false)
      {
        Directory.CreateDirectory(mCompressedFolder);
      }

      foreach (var item in mMusicFiles)
      {
        FileInfo sourcefi = new FileInfo(item);

        string mp3DestinationName = string.Format(@"{0}\{1}", mCompressedFolder, sourcefi.Name.Replace(".wav", ".mp3"));
        ConvertFile(item, mp3DestinationName);
      }

      MessageBox.Show("Complete", "Compression Status", MessageBoxButton.OK, MessageBoxImage.Information);
    }



    private void ConvertFile(string wavFileName, string mp3DestinationName)
    {

      byte[] mp3destination = ConvertWavToMp3(File.ReadAllBytes(wavFileName));
      File.WriteAllBytes(mp3DestinationName, mp3destination);
    }


    private byte[] ConvertWavToMp3(byte[] wavFile)
    {

      using (var retMs = new MemoryStream())
      using (var ms = new MemoryStream(wavFile))
      using (var rdr = new WaveFileReader(ms))
      using (var wtr = new LameMP3FileWriter(retMs, rdr.WaveFormat, LAMEPreset.ABR_256))
      {
        rdr.CopyTo(wtr);
        wtr.Flush();
        return retMs.ToArray();
      }


    }


  }
}
