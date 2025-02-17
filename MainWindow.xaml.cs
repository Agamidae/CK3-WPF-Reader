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
using System.Speech.Synthesis;
using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;

namespace CK3_WPF_Reader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int rate = 5;
        int volume = 100;
        int PercentRate = 0;
        string TextRate = "";
        string TextVolume = "";
        string voice = "";
        bool launched = false;

        public class Voices : ObservableCollection<string>
        {
            SpeechSynthesizer synth = new();
            public Voices()
            {
                foreach (InstalledVoice voice in synth.GetInstalledVoices())
                {
                    Add(voice.VoiceInfo.Name);
                }
                synth.Dispose();
            }
        }


        public MainWindow()
        {
            InitializeComponent();

            DisplayRate();

            SpeechSynthesizer synth = new()
            {
                Rate = rate,
                Volume = volume
            };

            synth.SetOutputToDefaultAudioDevice();

            foreach (InstalledVoice voice in synth.GetInstalledVoices())
            {
                VoiceBox.Items.Add(voice.VoiceInfo.Name);
            }

            VoiceBox.SelectedIndex = 0;

            //synth.SelectVoice(VoiceBox.SelectedItem.ToString());

            //System.Collections.ObjectModel.ReadOnlyCollection<InstalledVoice> installedVoices = synth.GetInstalledVoices();

            //VoiceBox.DataContext = synth;

            synth.Speak("Launching CK3 Reader");

            synth.Dispose();

            launched = true;
        }
    
        public void DisplayRate()
        {
            PercentRate = rate * 10 + 100;
            TextRate = "Speech Rate: " + PercentRate.ToString() + "%";
            SpeechRate.Content = TextRate;
        }

        public void DisplayVolume()
        {
            TextVolume = "Speech Volume: " + volume.ToString() + "%";
            SpeechVolume.Content = TextVolume;
        }

        public void SetupSpeech()
        {
            SpeechSynthesizer synth = new()
            {
                Rate = rate,
                Volume = volume
            };

            synth.SetOutputToDefaultAudioDevice();
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {
            SpeechSynthesizer synth = new()
            {
                Rate = rate,
                Volume = volume
            };

            synth.SetOutputToDefaultAudioDevice();

            synth.Speak(PercentRate.ToString());

            synth.Dispose();
 

            //if (DebugButton.IsChecked == true)
            //{
            //}
            //else if (ErrorButton.IsChecked == true)
            //{
            //    MessageBox.Show("Error log selected");
            //}
        }

        private void speechSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            rate = (int)e.NewValue;
            DisplayRate();
        }
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            volume = (int)e.NewValue;
            DisplayVolume();
        }

        public void VoiceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            voice = VoiceBox.SelectedItem.ToString();
            SpeechSynthesizer synth = new()
            {
                Rate = rate,
                Volume = volume
            };
            synth.SelectVoice(voice);
            synth.SetOutputToDefaultAudioDevice();
            if (launched == true)
            {
                synth.Speak(voice);
            }
            synth.Dispose();

        }
    }
}