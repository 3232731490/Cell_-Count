using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Media;
//using System.Drawing;
using System.Windows.Shapes;
using OpenCvSharp;
using System.Windows.Media.Media3D;
using System.IO;
using System.Xml;
using System.Windows.Threading;

namespace Cell__Count
{
    public partial class MainWindow : System.Windows.Window
    {

        List<Rectangle> myRects= new List<Rectangle>();   // 记录所画矩形
        List<TextBlock> textBlocks= new List<TextBlock>();  // 记录所标数字
        /// <summary>
        /// 选择输入图像目录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputImage_Click(object sender, RoutedEventArgs e)
        {
            if(ModeChoice.SelectedIndex== 0)
            {
                Open_Folder();
            }
            else
            {
                Open_File();
            }
        }
        /// <summary>
        /// 选择文件夹
        /// </summary>
        private void Open_Folder()
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;//设置为选择文件夹
            dialog.Title = "请选择图像所在文件夹";
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)   // 判断用户是否点击的确认
            {
                if (myRects.Count > 0) { Clear_all(); } // 画布不干净则清空画布
                this.myFile.File_BasePath = dialog.FileName;
                if (myFile.File_BasePath != "")
                {
                    myFile.read_file();    // 将当前文件夹中的所有图像文件读入
                    myFile.Len = myFile.Files.Count();    // 计算总图片数量
                }
                this.myFile.File_Name = this.myFile.get_curFile();  // 更新当前文件名
                this.myFile.Image_Path = this.myFile.File_BasePath + "\\" + this.myFile.File_Name;  // 由base路径和文件名组合成一个完整路径
                this.myFile.IsCheckedMode= false;
                this.myFile.IsInputMode= true;
                
            }
        }
        /// <summary>
        /// 选择文件
        /// </summary>
        private void Open_File()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;//该值确定是否可以选择多个文件
            dialog.Title = "请选择图像文件";
            dialog.Filter = "图像文件(*.jpg;*.jpeg;*.gif;*.png;*.bmp;*.tif;*.tiff)|*.jpg;*.jpeg;*.gif;*.png;*.bmpp;*.tif;*.tiff";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if(myRects.Count > 0) { Clear_all(); }  // 画布不干净则清空画布
                //this.myFile.File_BasePath = dialog.FileName;
                this.myFile.File_BasePath= System.IO.Path.GetDirectoryName(dialog.FileName);
                this.myFile.File_Name = dialog.SafeFileName;  // 更新当前文件名
                this.myFile.Image_Path = dialog.FileName;
                this.myFile.Len = 1;
                this.myFile.Checked_Num = 0;
                this.myFile.Cur_Pos = 0;
                this.myFile.IsCheckedMode= false;
                this.myFile.IsInputMode=false;
            }
        }
        /// <summary>
        /// 计数模式变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModeChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (myFile == null) return;
            // 批量模式
            if(ModeChoice.SelectedIndex == 0)
            {
                myFile.IsInputMode= true;
            }
            // 单个模式
            else
            {
               myFile.IsInputMode = false;
            }
        }
        /// <summary>
        /// 上一个图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pre_Image_Click(object sender, RoutedEventArgs e)
        {
            if (ModeChoice.SelectedIndex == 1 && (!myFile.IsCheckedMode||myFile.IsInputMode)) { ShowPopup("当前模式为单个模式，请更换模式后继续…"); return; }
            if (this.myFile.Len == 0)
            {
                ShowPopup("当前文件夹中不含有图片文件");
                return;
            }
            if (this.myFile.Cur_Pos > 0)    // 判断目前是否是第一张图片
            {
                // 选取上一个文件
                this.myFile.sub_pos();
                this.myFile.File_Name = this.myFile.get_curFile();
                this.myFile.Image_Path = this.myFile.File_BasePath + "\\" + this.myFile.File_Name;
                Read_XML_info(this.myFile.Image_Path);  // 重新读取信息
            }
            else
            {
                ShowPopup("当前已经为第一张样本图");
            }
        }
        /// <summary>
        /// 下一个图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Next_Image_Click(object sender, RoutedEventArgs e)
        {
            if (ModeChoice.SelectedIndex == 1 && (!myFile.IsCheckedMode || myFile.IsInputMode)) { ShowPopup("当前模式为单个模式，请更换模式后继续…"); return; }

            if (this.myFile.Len == 0)
            {
                ShowPopup("当前文件夹中不含有图片文件");
                return;
            }
            if (this.myFile.Cur_Pos < this.myFile.Len - 1)    // 判断目前是否为最后一张图片
            {
                // 选取下一张
                this.myFile.add_pos();
                this.myFile.File_Name = this.myFile.get_curFile();
                this.myFile.Image_Path = this.myFile.File_BasePath + "\\" + this.myFile.File_Name;
                Read_XML_info(this.myFile.Image_Path);  // 重新读取信息
            }
            else
            {
                ShowPopup("当前已经为最后一张样本图");
            }
        }
        /// <summary>
        /// 结果查看目录选择
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckImage_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;//设置为选择文件夹
            dialog.Title = "请选择图像所在文件夹";
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)   // 判断用户是否点击的确认
            {
                this.myFile.Files.Clear();
                this.myFile.Check_Path = dialog.FileName;
                this.myFile.File_Name = this.myFile.get_curFile();  // 更新当前文件名
                this.myFile.Image_Path = this.myFile.Check_Path + "\\" + this.myFile.File_Name;  // 由base路径和文件名组合成一个完整路径
                Read_XML_info(myFile.Image_Path);
            }
        }

        private void Clear_Rect()
        {
            if(myRects.Count <= 0) { return; }
            foreach (Rectangle rect in myRects) MyCanvas.Children.Remove(rect); // 删除矩形
            foreach (TextBlock t in textBlocks) MyCanvas.Children.Remove(t);    // 删除数字
        }
        
        private void Clear_all()
        {
            Clear_Rect();   // 删除矩形
            myCellInfos.Clear();    // 清空存储的细胞信息
            this.CurCellNum.Text = 0.ToString();     // 更新当前图像细胞数
        }

        /// <summary>
        /// 绘制矩形
        /// </summary>
        private void Draw_Rect()
        {
            double ratingW = 0.0;    // 图像宽放缩比例
            double ratingH = 0.0;    // 图像高放缩比例
            bool isFirstChild = true;
            Rectangle currect = new Rectangle();
            foreach (CellInfo cellInfo in myCellInfos)
            {
                // 第一个元素的宽高为原始图像宽高
                if (isFirstChild)
                {
                    ratingW = textBlock.ActualWidth / cellInfo.Width;    // 设置宽缩放比例
                    ratingH = textBlock.ActualHeight / cellInfo.Height;  // 设置高缩放比例
                    isFirstChild = false;
                    continue;
                }
                currect = new Rectangle();  // 当前矩形
                currect.Stroke = new SolidColorBrush(Colors.Red);   // 矩形边框颜色
                currect.Width = cellInfo.Width * ratingW;
                currect.Height = cellInfo.Height * ratingH;
                
                myRects.Add(currect);   // 将当前矩形添加至矩形列表
                Canvas.SetLeft(currect, cellInfo.PositionX * ratingW);
                Canvas.SetTop(currect, cellInfo.PositionY * ratingH);

                TextBlock tb = new TextBlock();
                // 设置 TextBlock 的属性
                tb.Text = cellInfo.Id; // 设置要显示的数字
                tb.FontSize = 16; // 设置字体大小
                tb.Foreground = Brushes.Green; // 设置字体颜色
                textBlocks.Add(tb);     // 将当前数字添加至列表
                // 设置 TextBlock 的位置
                Canvas.SetLeft(tb, cellInfo.PositionX * ratingW); // 设置左边距
                Canvas.SetTop(tb, cellInfo.PositionY * ratingH); // 设置上边距

                // 将 TextBlock和矩形矩形 添加到 Canvas 中
                MyCanvas.Children.Add(tb);
                MyCanvas.Children.Add(currect);
            }
        }


        private void Read_XML_info(string filename)
        {
            //TODO 读取图像信息的XML信息并在图像中标注细胞
            string directoryPath = System.IO.Path.GetDirectoryName(filename); // 获取文件目录
            string fileName = System.IO.Path.GetFileNameWithoutExtension(filename);   // 获取文件名(不带后缀)
            string xml_path = directoryPath + "\\" + MainWindow.XML_Path + "\\" + fileName + ".xml";
            if (!File.Exists(xml_path))  // 检查xml文件是否存在
            {
                Clear_all();
                ShowPopup("当前图像还未计数…");
                return;
            }
            XmlDocument doc = new XmlDocument();    // 创建一个 XmlDocument 对象
            doc.Load(xml_path);   // 读取 XmlDocument 对象
            XmlNode xn = doc.SelectSingleNode("CELLS");
            XmlNodeList cells = xn.ChildNodes;   // 所有细胞信息
            Clear_all();
            this.CurCellNum.Text = xn.Attributes["COUNT"].Value;    // 当前细胞总数
            Rectangle currect = new Rectangle();
            foreach (XmlNode cell in cells)
            {
                int POSITIONX = int.Parse(cell.SelectSingleNode("POSITIONX").InnerText); // 左上角x坐标
                int POSITIONY = int.Parse(cell.SelectSingleNode("POSITIONY").InnerText); // 左上角x坐标
                int WIDTH = int.Parse(cell.SelectSingleNode("WIDTH").InnerText); // 左上角x坐标
                int HEIGHT = int.Parse(cell.SelectSingleNode("HEIGHT").InnerText); // 左上角x坐标
                CellInfo cellInfo = new CellInfo();
                cellInfo.Width = WIDTH; 
                cellInfo.Height = HEIGHT;
                cellInfo.PositionX= POSITIONX;
                cellInfo.PositionY= POSITIONY;
                cellInfo.Id = cell.Attributes["ID"].Value;    // 矩形编号
                myCellInfos.Add(cellInfo);
            }
            Draw_Rect();
        }

        private void ShowConfirmDialog()
        {
            MyDialog dialog = new MyDialog(this);
            dialog.Owner = this;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.mainWindow.Background = new SolidColorBrush(Colors.Gray); ;
            dialog.ShowDialog();
            this.mainWindow.Background = new SolidColorBrush(Colors.LightBlue);
        }

        /// <summary>
        /// 气泡提示
        /// </summary>
        /// <param name="message">提示消息</param>
        private void ShowPopup(string message)
        {
            popupText.Text = message;
            popup.IsOpen = true;

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (sender, e) =>
            {
                timer.Stop();
                popup.IsOpen = false;
            };
            timer.Start();
        }
        /// <summary>
        /// 检查是否符合计数条件
        /// </summary>
        /// <returns></returns>
        private bool CheckValid()
        {
            if (myFile.Len == 0)
            {
                ShowPopup("未选择任何图像，无法计算…");
                return false;
            }
            if (myFile.IsCheckedMode)
            {
                ShowPopup("当前为查看模式，无需计算…");
                return false;
            }
            if (myFile.Checked_Num == myFile.Len)
            {
                ShowPopup("当前文件夹所有图像以计数完成，无需重复计数");
                return false; 
            }
            return true;
        }
        /// <summary>
        /// 开始计数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Btn_Run_Click(object sender, RoutedEventArgs e)
        {
            string curFileName = "";
            int curCellNum = 0;
            int i = 0;
            if (!CheckValid()) return;
            Algorithm algorithm = new Algorithm();
            Algorithm.MyDelegate myFunction = null; // 新建委托实例
            ComboBoxItem selectedItem = ModeComboBox.SelectedItem as ComboBoxItem;
            string mode = selectedItem.Content.ToString();
            if(mode == "连通域分割")
                myFunction = new Algorithm.MyDelegate(algorithm.Connected_solve);
            else
                myFunction = new Algorithm.MyDelegate(algorithm.Water_Algorithm_solve);
            // 检查是否存在存储XML信息文件夹，若不存在则创建
            string folderPath = myFile.File_BasePath+"\\" + XML_Path;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            if (myFile.IsInputMode) // 批量模式
            {
                // 依次对当前文件夹内容进行计数
                foreach (var file in myFile.Files)
                {
                    curFileName = myFile.File_BasePath + "//" + file;
                    this.myFile.Image_Path= curFileName;    // 更新UI显示图片
                    this.myFile.File_Name = file;           // 更新当前文件名
                    curCellNum = algorithm._solve(myFunction,curFileName);
                    this.CurCellNum.Text = curCellNum.ToString();     // 更新当前图像细胞数
                    myFile.Checked_Num++;
                    myFile.Progress = (int)(100 * (double)myFile.Checked_Num / myFile.Len);
                    await Task.Delay(100);
                }
                this.myFile.Cur_Pos = this.myFile.Checked_Num - 1;
            }
            // 单个模式
            else
            {
                curCellNum = algorithm._solve(myFunction,this.myFile.Image_Path);
                this.CurCellNum.Text = curCellNum.ToString();     // 更新当前图像细胞数
                myFile.Checked_Num++;
                myFile.Progress = (int)(100 * (double)myFile.Checked_Num / myFile.Len);
                await Task.Delay(100);
            }
            ShowPopup("计数已完成");
        }

        /// <summary>
        /// 算法下拉框更改提示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (myFile == null) return;    // 刚进入软件时无需显示此消息
            ComboBoxItem selectedItem = ModeComboBox.SelectedItem as ComboBoxItem;
            string mode = selectedItem.Content.ToString();
            // 根据选择的模式执行相应操作
            switch (mode)
            {
                case "连通域分割":
                    ShowPopup("当前所用算法基于连通域分割，速度较快但误差较大");
                    break;
                case "分水岭分割":
                    ShowPopup("当前所用算法基于分水岭分割，对于特定图像准确率有所提高，但速度较慢");
                    break;
                default:
                    break;
            }
        }
    }
}
