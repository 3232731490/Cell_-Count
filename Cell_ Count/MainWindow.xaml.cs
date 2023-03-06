using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using static Cell__Count.MainWindow;

namespace Cell__Count
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string XML_Path = "Cell_Info_XML";
        MyFile myFile,checkFile;
        List<CellInfo> myCellInfos= new List<CellInfo>();
        public MainWindow()
        {
            InitializeComponent();
            btn_min.Click += (s, e) => { this.WindowState = WindowState.Minimized; };
            btn_max.Click += (s, e) => { 
                if(this.WindowState == WindowState.Maximized)
                    this.WindowState = WindowState.Normal;
                else
                    this.WindowState = WindowState.Maximized;
                Clear_Rect();   // 清空矩形
                Draw_Rect();    // 重绘矩形
            };
            btn_close.Click += (s, e) => {
                ShowConfirmDialog();
            };
            Top.MouseMove += (s, e) => {
                if (e.LeftButton == MouseButtonState.Pressed)
                    this.DragMove();
            };
            myFile = new MyFile() { File_BasePath = "" };
            checkFile = new MyFile() { File_BasePath = "" };
            // 数据绑定
            this.InputImage_Text.SetBinding(TextBox.TextProperty, new Binding("File_BasePath") { Source = myFile });
            this.Img.SetBinding(Image.SourceProperty, new Binding("Image_Path") { Source = myFile });
            this.CurFileName.SetBinding(TextBlock.TextProperty, new Binding("File_Name") { Source = myFile});
            this.CheckImage_Text.SetBinding(TextBox.TextProperty, new Binding("Check_Path") { Source = myFile });
            this.CheckedNum.SetBinding(TextBlock.TextProperty, new Binding("Checked_Num") { Source = myFile });
            this.Pros_Bar.SetBinding(ProgressBar.ValueProperty, new Binding("Progress") { Source = myFile });
        }

        /// <summary>
        /// 保存文件信息
        /// </summary>
        public class MyFile : INotifyPropertyChanged
        {
            private string file_basepath;
            private string file_name;
            private string image_path;
            private string check_path;
            private bool isCheckedMode = false;
            private bool isInputMode = false;
            private double progress = 0.0;
            private int checked_num = 0;
            private int cur_pos = 0;
            private bool isOutputPath = false;
            public int Len { get; set; }
            public bool IsOutputPath
            {
                get { return isOutputPath; }
                set { isOutputPath = value; }
            }
            public double Progress
            {
                get { return progress; }
                set { progress = value; OnPropertyChanged("progress"); }
            }
            public int Checked_Num
            {
                get { return checked_num; } set { checked_num = value; OnPropertyChanged("checked_num"); }
            }
            public int Cur_Pos
            {
                set { 
                    cur_pos = value;
                    OnPropertyChanged("cur_pos"); // 通知前端界面改变
                }
                    
                    get { return cur_pos; } }
            public void add_pos() { cur_pos++; }
            public void sub_pos() { cur_pos--; }
            public bool IsCheckedMode
            {
                set { isCheckedMode = value; }
                get { return isCheckedMode; }
            }
            public bool IsInputMode
            {
                set { isInputMode = value; }
                get { return isInputMode; }
            }
            public String Check_Path
            {
                set {
                    check_path = value;
                    file_basepath = value;
                    isCheckedMode = true;
                    isInputMode = false;
                    OnPropertyChanged("check_path"); // 通知前端界面改变
                    this.files.Clear(); // 清除目前已保存的文件夹中的文件名
                    DirectoryInfo folder = new DirectoryInfo(check_path);
                    var fs = folder.GetFiles();
                    Regex re = new Regex("^*.(jpg|png|bmp|gif|jpeg|tif|tiff)$"); // 判断是否为图像
                    foreach (FileInfo file in fs)
                    {
                        if (re.IsMatch(file.Name))
                            files.Add(file.Name);
                    }
                    Len = files.Count;
                    cur_pos = 0;
                }
                get { return check_path; }
            }
            public String File_BasePath
            {
                set
                { // 如果base路径发生变化则通知前端界面做出相应改变
                    file_basepath = value;
                    isInputMode = true;
                    isCheckedMode = false;
                    OnPropertyChanged("file_basepath"); // 通知前端界面改变
                    this.files.Clear(); // 清除目前已保存的文件夹中的文件名
                    cur_pos = 0;
                    Checked_Num = 0;
                    Progress = 0.0;
                    isCheckedMode = false;
                }
                get { return file_basepath; }
            }

            /// <summary>
            /// 读取当前文件夹中的所有图片并保存起来
            /// </summary>
            public void read_file()
            {
                DirectoryInfo folder = new DirectoryInfo(file_basepath);
                var fs = folder.GetFiles();
                Regex re = new Regex("^*.(jpg|png|bmp|gif|jpeg|tif|tiff)$"); // 判断是否为图像
                foreach (FileInfo file in fs)
                {
                    if (re.IsMatch(file.Name))
                        files.Add(file.Name);
                }
            }

            public String File_Name
            {
                set
                {
                    file_name = value;
                    OnPropertyChanged("file_name"); // 通知前端界面做出改变
                }
                get
                {
                    return file_name;
                }
            }
            public String Image_Path
            {
                set
                {
                    image_path = value;
                    OnPropertyChanged("image_path");    // 通知前端界面做出改变
                }
                get
                {
                    return image_path;
                }
            }

            public string get_curFile()
            {
                return Len == 0 ? "" : this.files[cur_pos]; // 返回当前文件的文件名
            }

            protected void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));  // 通知前端界面改变
                }
            }
            private List<String> files = new List<string>();    // 保存当前文件所有文件名
            public List<String> Files { get { return files; } }
            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class CellPoint
        {
            private int positionX;
            private int positionY;
            private int width;
            private int height;
            private int id;

            public int PositionX { get { return positionX; } set { positionX = value; } }
            public int Width { get { return width; } set { width = value; } }
            public int Height { get { return height; } set { height = value; } }
            public int PositionY { get { return positionY; } set { positionY = value; } }
            public int Id { get { return id; } set { id = value; } }
        }

        ///// <summary>
        ///// 保存瑕疵区域的信息
        ///// </summary>
        public class CellInfo
        {
            private int positionX;
            private int positionY;
            private double width;
            private double height;
            private string id;
            public double Width { get; set; }
            public double Height { get; set; }
            public int PositionX { get; set; }
            public int PositionY { get; set; }
            public string Id { get { return id; }set { id = value; } }
        }
    }
}
