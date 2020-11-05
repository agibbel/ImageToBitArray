using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageToBitArray
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Документация
        /// </summary>
        public string Documentation { get; } = @"Добрый день!

Данная программа предназначена для преобразования монохромного изображения в структуру данных формата языка C/C++ и подобных. Вы можете ограничить изображение только значащими строками/столбцами, что способствует уменьшению размера формируемого массива.

Формируемая структура имеет вид:
struct
{
    uint16_t posx;      // Позиция по X
    uint16_t posy;      // Позиция по Y
    uint16_t width;     // Ширина изображения
    uint16_t height;    // Высота изображения
    uint32_t data[];    // Данные
};

Пример использования:
https://gist.github.com/agibbel/670a4201cc758dcda02273323c14d5c9

Copyright (C) 2020, Andrey Tepliakov
Программа распространяется под лицензией GNU GPL-3.0
Репозиторий с исходным кодом:
https://github.com/agibbel/ImageToBitArray
";

        /// <summary>
        /// Имя файла изображения
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set
            {
                if (value != _fileName)
                {
                    _fileName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileName)));
                    LoadImage();
                }
            }
        }
        private string _fileName = string.Empty;

        /// <summary>
        /// Инверсия
        /// </summary>
        public bool Inverse
        {
            get => _inverse;
            set
            {
                if (value != _inverse)
                {
                    _inverse = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Inverse)));
                    UpdatePreview();
                }
            }
        }
        private bool _inverse = false;

        /// <summary>
        /// Уровень
        /// </summary>
        public int Treshold
        {
            get => _treshold;
            set
            {
                if (value != _treshold)
                {
                    _treshold = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Treshold)));
                    UpdatePreview();
                }
            }
        }
        private int _treshold = 127;

        /// <summary>
        /// Обрезать пустые
        /// </summary>
        public bool NoEmpty
        {
            get => _noEmpty;
            set
            {
                if (value != _noEmpty)
                {
                    _noEmpty = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NoEmpty)));
                    UpdatePreview();
                }
            }
        }
        private bool _noEmpty = false;

        /// <summary>
        /// Непрерывные данные (поток бит не ограничивается окончанием строки
        /// </summary>
        public bool NonStopData
        {
            get => _nonStopData;
            set
            {
                if (value != _nonStopData)
                {
                    _nonStopData = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NonStopData)));
                    UpdatePreview();
                }
            }
        }
        private bool _nonStopData = false;

        /// <summary>
        /// Цвет изображения для предпросмотра
        /// </summary>
        public string Foreground
        {
            get => "#" + _foreground.ToString("X6");
            set
            {
                if (value.Length == 7 && value[0] == '#')
                {
                    _foreground = Convert.ToUInt32(value.Substring(1), 16);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Foreground)));
                    UpdatePreview();
                }
            }
        }
        private uint _foreground = 0x000000;

        /// <summary>
        /// Цвет фона для предпросмотра
        /// </summary>
        public string Background
        {
            get => "#" + _background.ToString("X6");
            set
            {
                if (value.Length == 7 && value[0] == '#')
                {
                    _background = Convert.ToUInt32(value.Substring(1), 16);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Background)));
                    UpdatePreview();
                }
            }
        }
        private uint _background = 0xFFFFFF;

        /// <summary>
        /// Результат обработки
        /// </summary>
        public string Result
        {
            get => _result;
            set
            {
                if (value != _result)
                {
                    _result = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
                }
            }
        }
        private string _result;

        /// <summary>
        /// Изображение для предпросмотра
        /// </summary>
        public BitmapImage Preview
        {
            get => _preview;
            set
            {
                if (value != _preview)
                {
                    _preview = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Preview)));
                }
            }
        }
        private BitmapImage _preview;

        /// <summary>
        /// Конвертирует Bitmap в BitmapImage
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private BitmapImage ConvertBitmapToBitmapImage(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            src.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }

        /// <summary>
        /// Загрузка изображения
        /// </summary>
        private async void LoadImage()
        {
            try
            {
                PixelsInfo = await Task.Run(() => ImagePixelsInfo.FromFile(FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка открытия файла : " + (ex.InnerException ?? ex).Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обновление полей предпросмотра
        /// </summary>
        private async void UpdatePreview()
        {
            try
            {
                if (PixelsInfo is ImagePixelsInfo && PixelsInfo.Height > 0 && PixelsInfo.Width > 0)
                {
                    ushort Height = PixelsInfo.Height;
                    ushort Width = PixelsInfo.Width;
                    ushort Y = 0;
                    ushort X = 0;
                    bool[,] pixels = new bool[Height, Width];
                    bool[] notEmptyRows = new bool[Height];
                    bool[] notEmptyColumns = new bool[Width];
                    await Task.Run(() =>
                    {
                        for (ushort y = 0; y < Height; y++)
                            for (ushort x = 0; x < Width; x++)
                            {
                                bool pixel = (Inverse) ? (PixelsInfo.Pixels[y, x] > Treshold) : (PixelsInfo.Pixels[y, x] < Treshold);
                                pixels[y, x] = pixel;
                                notEmptyRows[y] |= pixel;
                                notEmptyColumns[x] |= pixel;
                            }
                        if (NoEmpty)
                        {
                            for (ushort y = 0; y < Height; y++)
                            {
                                if (notEmptyRows[y])
                                    break;
                                Y++;
                            }
                            for (ushort y = (ushort)(Height - 1); y >= Y; y--)
                            {
                                if (notEmptyRows[y])
                                    break;
                                Height--;
                            }
                            Height -= Y;
                            for (ushort x = 0; x < Width; x++)
                            {
                                if (notEmptyColumns[x])
                                    break;
                                X++;
                            }
                            for (ushort x = (ushort)(Width - 1); x >= X; x--)
                            {
                                if (notEmptyColumns[x])
                                    break;
                                Width--;
                            }
                            Width -= X;
                        }
                    });
                    using Bitmap previewBitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
                    string result = "{\r\n\t" + X.ToString() + ",\r\n\t" + Y.ToString() + ",\r\n\t" + Width.ToString() + ",\r\n\t" + Height.ToString() + ",\r\n\t{";
                    if (NonStopData)
                    {
                        int value = 0;
                        ushort y = 0;
                        ushort x = 0;
                        result += "\r\n\t\t";
                        for (y = 0; y < Height; y++)
                        {
                            for (x = 0; x < Width; x++)
                            {
                                previewBitmap.SetPixel(x, y, Color.FromArgb(Convert.ToInt32(pixels[y + Y, x + X] ? _foreground : _background)));
                                if (pixels[y + Y, x + X])
                                    value |= 1 << ((x + y * Width) & 31);
                                if (((x + y * Width) & 31) == 31)
                                {
                                    result += "0x" + value.ToString("X") + ", ";
                                    value = 0;
                                }
                            }
                        }
                        if (((x + y * Width) & 31) != 31)
                            result += "0x" + value.ToString("X") + ", ";
                    }
                    else
                    {
                        for (ushort y = 0; y < Height; y++)
                        {
                            result += "\r\n\t\t";
                            int value = 0;
                            ushort x = 0;
                            for (x = 0; x < Width; x++)
                            {
                                previewBitmap.SetPixel(x, y, Color.FromArgb(Convert.ToInt32(pixels[y + Y, x + X] ? _foreground : _background)));
                                if (pixels[y + Y, x + X])
                                    value |= 1 << (x & 31);
                                if ((x & 31) == 31)
                                {
                                    result += "0x" + value.ToString("X") + ", ";
                                    value = 0;
                                }
                            }
                            if ((x & 31) != 31)
                                result += "0x" + value.ToString("X") + ", ";
                        }
                    }
                    if (result[^1] == ' ' && result[^2] == ',')
                        result = result[0..^2];
                    result += "\r\n\t}\r\n}";
                    Preview = ConvertBitmapToBitmapImage(previewBitmap);
                    Result = result;
                }
                else
                {
                    Preview = null;
                    Result = string.Empty;
                }
            }
            catch
            {
                Preview = null;
                Result = "Ошибка преобразования";
            }
        }

        /// <summary>
        /// Информация о точках изображения
        /// </summary>
        private ImagePixelsInfo PixelsInfo
        {
            get => _pixelsInfo;
            set
            {
                _pixelsInfo = value;
                UpdatePreview();
            }
        }
        private ImagePixelsInfo _pixelsInfo;


        /// <summary>
        /// Получение информации о точках изображения
        /// </summary>
        private class ImagePixelsInfo
        {
            /// <summary>
            /// Ширина
            /// </summary>
            public ushort Width;
            /// <summary>
            /// Высота
            /// </summary>
            public ushort Height;
            /// <summary>
            /// Оттенок точек
            /// </summary>
            public byte[,] Pixels;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="Width">Ширина</param>
            /// <param name="Height">Высота</param>
            public ImagePixelsInfo(ushort Width, ushort Height)
            {
                this.Width = Width;
                this.Height = Height;
                Pixels = new byte[Height, Width];
            }

            /// <summary>
            /// Заполняет массив данными изображения
            /// </summary>
            /// <param name="image">Изображение</param>
            public void LoadImage(Image image)
            {
                using Bitmap grayscaleBitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
                using Graphics g = Graphics.FromImage(grayscaleBitmap);
                {
                    ColorMatrix colorMatrix = new ColorMatrix(
                        new float[][]
                        {
                                    new float[] {.3f, .3f, .3f, 0, 0},
                                    new float[] {.59f, .59f, .59f, 0, 0},
                                    new float[] {.11f, .11f, .11f, 0, 0},
                                    new float[] {0, 0, 0, 1, 0},
                                    new float[] {0, 0, 0, 0, 1}
                        });
                    using ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(image, new Rectangle(0, 0, Width, Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                }
                for (ushort y = 0; y < Height; y++)
                    for (ushort x = 0; x < Width; x++)
                        Pixels[y, x] = grayscaleBitmap.GetPixel(x, y).R;
            }

            /// <summary>
            /// Создает объект из файла с изображением
            /// </summary>
            /// <param name="filename">Имя файла</param>
            /// <returns></returns>
            public static ImagePixelsInfo FromFile(string filename)
            {
                using Image originalImage = Image.FromFile(filename);
                ImagePixelsInfo ipi = new ImagePixelsInfo(Convert.ToUInt16(originalImage.Width), Convert.ToUInt16(originalImage.Height));
                ipi.LoadImage(originalImage);
                return ipi;
            }
        }
    }
}
