using Microsoft.WindowsAPICodePack.Dialogs;
using NAudio.Lame;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

      CompressionStatus.Text = "";
      CompressProgress.Value = 0;
      //mMusicFiles = new List<string>();
      mSettings = MusicSquashSettings.Load();
      //m//CompressedFolder = "";

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

      CompressWorker = new BackgroundWorker
      {
        WorkerReportsProgress = true
      };
      CompressWorker.DoWork += CompressWorker_DoWork;
      CompressWorker.ProgressChanged += CompressWorker_ProgressChanged;
      CompressWorker.RunWorkerCompleted += CompressWorker_RunWorkerCompleted;


    }

    private int mTotalFiles;
    private DirectoryInfo mRootFolder;
    // private List<string> mMusicFiles;
    private MusicSquashSettings mSettings;
    //private string mCompressedFolder;
    private System.ComponentModel.BackgroundWorker CompressWorker;


    private void FolderChange_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new CommonOpenFileDialog
      {
        IsFolderPicker = true
      };

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
           

      //needs to work with multiple folders

      //Get folders selected

      var folders = FolderlistBox.SelectedItems;
      List<string> wavFiles = new List<string>();
      foreach (var folder in folders)
      {
        DirectoryInfo sourceFolder = new DirectoryInfo(string.Format(@"{0}\{1}", mRootFolder.FullName, folder));
        wavFiles.AddRange(Directory.GetFiles(sourceFolder.FullName, "*.wav").ToList());
      }

      mTotalFiles = wavFiles.Count;

      if (mTotalFiles > 0)
      {
        if (MessageBox.Show(string.Format("Converting {0} Wav file(s).\nAre You Sure?", mTotalFiles), "Confirm Compression",
          MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
          //disable btns
          CompressButton.IsEnabled = false;
          CloseButton.IsEnabled = false;

          CompressWorker.RunWorkerAsync(wavFiles);
        }



      }
    }

    private void CompressWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      MessageBox.Show("Complete", "Compression Status", MessageBoxButton.OK, MessageBoxImage.Information);

      //enable btns
      CompressionStatus.Text = "";
      CompressProgress.Value = 0;
      CompressButton.IsEnabled = true;
      CloseButton.IsEnabled = true;

    }

    private void CompressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
      double progress100 = e.ProgressPercentage * 100 / mTotalFiles ;
      CompressionStatus.Text = string.Format("Processing file {0} out of {1} ({2}%)", e.ProgressPercentage, mTotalFiles, progress100);
      CompressProgress.Value = progress100;
    }


    private void CompressWorker_DoWork(object sender, DoWorkEventArgs e)
    {
      ConvertFiles((List<string>)e.Argument);
    }

    private void FolderlistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (FolderlistBox.SelectedIndex > -1)
      {
        CompressButton.IsEnabled = true;
      }
      else
      {
        CompressButton.IsEnabled = false;
      }

    }

    private void ConvertFiles(List<string> wavFiles)
    {

      //a list of files that may or may not exist.
      //create the 
      int i = 0;

      foreach (string wavFile in wavFiles)
      {
        FileInfo wav = new FileInfo(wavFile);
        DirectoryInfo dir = wav.Directory;

        //create folder if neccessary
        string compressedFolder = string.Format(@"{0}\compressed-{1}", dir.Parent.FullName, dir.Name);
        if (Directory.Exists(compressedFolder) == false)
        {
          Directory.CreateDirectory(compressedFolder);
        }

        //convert file
        string mp3DestinationName = string.Format(@"{0}\{1}", compressedFolder, wav.Name.Replace(".wav", ".mp3"));
        if (!File.Exists(mp3DestinationName))
        { 
        ConvertFile(wav.FullName, mp3DestinationName);
        }
        CompressWorker.ReportProgress(++i);
      }

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
      using (var wtr = new LameMP3FileWriter(retMs, rdr.WaveFormat, LAMEPreset.ABR_320))
      {
        rdr.CopyTo(wtr);
        wtr.Flush();
        return retMs.ToArray();
      }


    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }


  }
}
