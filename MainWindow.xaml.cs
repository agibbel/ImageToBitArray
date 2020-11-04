using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ImageToBitArray
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel = new MainWindowViewModel();
        
        public MainWindow()
        {
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void FileName_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.FileName))
                FileName_FileDialog();
        }

        private void FileName_MouseDoubleClick(object sender, MouseButtonEventArgs e) => FileName_FileDialog();

        private void FileName_FileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Изображения (*.png; *.jpg; *.bmp)|*.png;*.jpg;*.bmp|Все файлы (*.*)|*.*",
                InitialDirectory = Directory.GetCurrentDirectory(),
                Title = "Выберите файл загрузчика",
            };
            if (openFileDialog.ShowDialog() ?? false)
                ViewModel.FileName = openFileDialog.FileName;
        }
    }
}
