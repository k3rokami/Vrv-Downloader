using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VrvDownloader.Models;
using VrvDownloader.Progress;
using VrvDownloader.Views;
using Newtonsoft.Json;
using static System.Globalization.CultureInfo;
using static VrvDownloader.Quality;

namespace VrvDownloader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase<DownloaderModel>
    {
        public MainWindowViewModel() : this(null)
        {

        }

        public MainWindowViewModel(DownloaderModel model = null) : base(model)
        {
        }

        public string DlStatus
        {
            get => Model.DlStatus;
            set => SetModelProperty(value);
        }

        public string[] AvailableFormats { get; } =
        {
            "srt",
            "ass"
		};

        public string Format
        {
            get => Model.Format;
            set => SetModelProperty(value);
        }

        public string Language
        {
            get => Model.Language;
            set => SetModelProperty(value);
        }

        public bool IsMkv
        {
            get => Model.IsMkv;
            set => SetModelProperty(value);
        }

        public QualityItem Quality
        {
            get => new QualityItem(Model.Quality);
            set => SetModelProperty(value.InnerQuality);
        }

        public string SavePath
        {
            get => Model.SavePath;
            set => SetModelProperty(value);
        }

        public bool AreSubtitlesEnabled
        {
            get => Model.AreSubtitlesEnabled;
            set => SetModelProperty(value);
        }

        public bool PremiumEnabled
        {
            get => Model.PremiumEnabled;
            set => SetModelProperty(value);
        }

        public string Url
        {
            get => Model.Url;
            set => SetModelProperty(value);
        }
        private int _selectedQualityItemIndex = 0;

        public int SelectedQualityItemIndex
        {
            get => _selectedQualityItemIndex;
            set
            {
                Set(value, out _selectedQualityItemIndex);
                Quality = QualityItem.AllQualities[value];
            }
        }

        private QualityItem _selectedQualityItem;

        public QualityItem SelectedQualityItem
        {
            get => QualityItem.AllQualities[_selectedQualityItemIndex];
            set => SelectedQualityItemIndex = Array.IndexOf(QualityItem.AllQualities, value);
        }

        public List<CultureInfo> AvailableLanguages { get; } = new List<CultureInfo>
        {
            GetCultureInfo("en-US"),
            GetCultureInfo("fr-FR"),
            GetCultureInfo("es-ES"),
            GetCultureInfo("es-LA"),
            GetCultureInfo("pt-BR"),
            GetCultureInfo("it-IT"),
            GetCultureInfo("de-DE"),
            GetCultureInfo("ru-RU")
        };
        private CultureInfo _selectedCultureInfo;

        public CultureInfo SelectedLanguage
        {
            get
            {
                if (_selectedCultureInfo is null)
                {
                    _selectedCultureInfo = AvailableLanguages[0];
                }
                return _selectedCultureInfo;
            }
            set
            {
                Set(value, out _selectedCultureInfo);
                Language = new string(_selectedCultureInfo.Name.Where(c => c != '-').ToArray());
            }
        }

        public async Task Download()
        {
            var process = GetYoutubeDlProcessWithArguments();
            var isIntentional = false;
            var data = new ProgressViewModel
            {
                IsIndeterminate = true,
                Progress = new TaskManager(new[]
                {
                    new ProgressTask("Fetching video info", 1) { Display = "{3}..." },
                    new ProgressTask("Downloading") { Display = "{3}... {2}" }
                })
                { FinalizingText = "Writing to file..." },
                CanClose = () => true,
                OnClose = () =>
                {
                    isIntentional = true;
                    //Process.Start("taskkill", "/F /IM ffmpeg.exe"); Old Task kill wif cmd
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "taskkill";
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.Arguments = "/F /IM ffmpeg.exe";
                    Process.Start(startInfo);
                }
            };
            var downloadWindow = new ProgressWindow(data);
            _ = Task.Run(() => Application.Current.Dispatcher.Invoke(() => downloadWindow.ShowDialog()));
            UpdateWithProcessOutput(process, data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Run(() => process.WaitForExit()); // do not block other threads.
            downloadWindow.Close();
            if (!isIntentional)
            {
                if (process.ExitCode == 0)
                    MessageBox.Show("Download finished !", "Success !", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Something wrong has happened while downloading the video. " +
                                    "Please check your URL", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void UpdateWithProcessOutput(Process process, ProgressViewModel data)
        {
            TimeSpan? totalDuration = null;
            const string pattern = "([0-9.:]+)";
            process.ErrorDataReceived += (_, e) =>
            {
                Debug.WriteLine("E: " + (e?.Data ?? "[null]"));
                if (e?.Data is null) return;
                if (totalDuration is null && e.Data.StartsWith("  Duration:"))
                {
                    var value = Regex.Match(e.Data, $"Duration: {pattern}").Groups[1].Value;
                    if (TimeSpan.TryParse(value, out var d))
                    {
                        totalDuration = d;
                        data.Progress.GoNext();
                        data.IsIndeterminate = false;
                    }
                }

                if (e.Data.StartsWith("frame="))
                {
                    var value = Regex.Match(e.Data, $"time={pattern}").Groups[1].Value;
                    if (TimeSpan.TryParse(value, out var step))
                    {
                        var progress = step.TotalMilliseconds / totalDuration.Value.TotalMilliseconds;
                        data.Progress.CurrentTask.Progress = progress;
                    }
                }
            };
            process.OutputDataReceived += (_, e) => { Debug.WriteLine("O { " + e.Data + " }"); };
        }

        private Process GetYoutubeDlProcessWithArguments()
        {
            var loginJson = File.ReadAllText(@"C:\ProgramData\Vrv-DL\login.json");
            dynamic loginIdents = JsonConvert.DeserializeObject(loginJson);
            string username = loginIdents.username;
            string password = loginIdents.password;

            var currentDirectory = Directory.GetCurrentDirectory();
            var currentSavePath = SavePath;
            var currentSavePath2 = currentDirectory;
            
            
            if (string.IsNullOrEmpty(SavePath))
            {
                currentSavePath2 += @"\%(title)s.%(ext)s";
                currentSavePath = @"";
            }
            else
            {
                currentSavePath2 = @"";
                currentSavePath += @"\%(title)s.%(ext)s";
            }
            var login = $"--username {username} --password {password}";
            var ua = $"--user-agent \"Mozilla / 5.0 (Windows NT 10.0; Win64; x64; rv: 65.0) Gecko / 20100101 Firefox / 65.0\"";
            var basicArguments = $"--console-title --newline --no-warnings --no-check-certificate --no-part -o  \"{currentSavePath}{currentSavePath2}\" {ua}";
            var subsArguments = $"--write-sub --sub-lang {Language} --sub-format {Format}";
            var qualityArguments = Quality == Best ? "-f best" : $"-f \"best[height={Quality}]\"";

            var process = new Process
            {
                StartInfo =
                {
                    FileName = @"C:\ProgramData\Vrv-DL\youtube-dl.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };

            if (PremiumEnabled & AreSubtitlesEnabled)
            {
                process.StartInfo.Arguments = $"{login} {qualityArguments} {subsArguments} {basicArguments}";
                
            }
            else if (PremiumEnabled)
            {
                process.StartInfo.Arguments = $"{login} {qualityArguments} {basicArguments}";
            }
            else if (AreSubtitlesEnabled)
            {
                process.StartInfo.Arguments = IsMkv
                    ? $"{qualityArguments} --recode-video mkv --embed-subs --postprocessor-args \"-disposition:s:0 default\" {subsArguments} {basicArguments}"
                    : $"{qualityArguments} {subsArguments} {basicArguments}";
            }
            else
            {
                process.StartInfo.Arguments = $"{qualityArguments} {subsArguments} {basicArguments}";
            }

            process.StartInfo.Arguments += " -v --newline " + Url;
            return process;
        }
    }
}