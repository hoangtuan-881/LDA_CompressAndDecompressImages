using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Accord.MachineLearning;
using Accord.Statistics.Analysis;
using Accord.Math;
using Accord.Math.Distances;

namespace ldaimage
{
    public partial class Form1 : Form
    {
        Bitmap originalBitmap = null;
        Bitmap grayscaleBitmap = null;
        string originalFilePath = null;
        public Form1()
        {
            InitializeComponent();
            numBlockSize.Value = 8;
            numClustersK.Value = 16;
            numLdaComponents.Value = 5;
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*";
            ofd.Title = "Chọn tập tin hình ảnh"; 
            try
            {
                string startupPath = AppDomain.CurrentDomain.BaseDirectory;
                string inputDirPath = Path.Combine(startupPath, "input");
                if (Directory.Exists(inputDirPath))
                {
                    ofd.InitialDirectory = inputDirPath;
                }
                else
                {
                    Console.WriteLine($"Cảnh báo: Không tìm thấy thư mục đầu vào tại '{inputDirPath}'. Sử dụng thư mục mặc định.");
                    // ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    //
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xác định thư mục ban đầu: {ex.Message}");
            }

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    pictureBoxOriginal.Image?.Dispose();
                    pictureBoxReconstructed.Image?.Dispose();
                    originalBitmap?.Dispose();
                    grayscaleBitmap?.Dispose();

                    using (Bitmap tempBitmap = new Bitmap(ofd.FileName))
                    {
                        originalBitmap = new Bitmap(tempBitmap);
                    }

                    pictureBoxOriginal.Image = new Bitmap(originalBitmap);
                    this.originalFilePath = ofd.FileName;
                    lblStatus.Text = $"Đã tải: {Path.GetFileName(ofd.FileName)}";
                    grayscaleBitmap = ConvertToGrayscale(originalBitmap);
                    pictureBoxReconstructed.Image = null;

                    lblStatus.Text = "Ảnh đã được tải. Sẵn sàng để xử lý.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tải hoặc xử lý hình ảnh: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    pictureBoxOriginal.Image?.Dispose();
                    pictureBoxReconstructed.Image?.Dispose();
                    originalBitmap?.Dispose();
                    grayscaleBitmap?.Dispose();

                    originalBitmap = null;
                    grayscaleBitmap = null;
                    pictureBoxOriginal.Image = null;
                    pictureBoxReconstructed.Image = null;
                    lblStatus.Text = "Tải hình ảnh thất bại.";
                    this.originalFilePath = null;
                }
            }
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            if (grayscaleBitmap == null)
            {
                MessageBox.Show("Vui lòng tải hình ảnh trước.", "Chưa có ảnh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int blockSize = (int)numBlockSize.Value;
            int k = (int)numClustersK.Value;
            int ldaComponents = (int)numLdaComponents.Value;

            if (ldaComponents >= k)
            {
                MessageBox.Show($"Số lượng thành phần LDA (d={ldaComponents}) phải nhỏ hơn K (K={k}).\nVui lòng đặt d < K.",
                                "Tham số không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (blockSize <= 0 || blockSize > grayscaleBitmap.Width || blockSize > grayscaleBitmap.Height)
            {
                MessageBox.Show("Kích thước khối không hợp lệ.", "ham số không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetUIEnabled(false);
            progressBar.Visible = true;
            progressBar.Value = 0;
            lblStatus.Text = "Đang bắt đầu xử lý...";
            pictureBoxReconstructed.Image?.Dispose();
            pictureBoxReconstructed.Image = null;
            Stopwatch stopwatch = Stopwatch.StartNew();

            Bitmap reconstructedImage = null;

            try
            {
                reconstructedImage = await Task.Run<Bitmap>(() =>
                {
                    UpdateStatus("Đang trích xuất và vector hóa các khối...", 10);
                    List<double[]> currentBlockVectors = ExtractAndVectorizeBlocks(grayscaleBitmap, blockSize);
                    if (currentBlockVectors == null || currentBlockVectors.Count == 0) { throw new Exception("Không thể trích xuất các khối hoặc không tìm thấy khối nào."); }
                    if (currentBlockVectors.Count < k) { throw new Exception($"Số lượng khối ({currentBlockVectors.Count}) nhỏ hơn K ({k}). Không thể thực hiện K-Means."); }

                    double[][] dataVectors = currentBlockVectors.ToArray();
                    int vectorDim = dataVectors[0].Length;

                    UpdateStatus($"Đang chạy K-Means (K={k})...", 30);
                    KMeans kmeans = new KMeans(k) { Distance = new Accord.Math.Distances.SquareEuclidean(), Tolerance = 0.01 };
                    KMeansClusterCollection clusters = null;
                    int[] currentClusterLabels = null;
                    try
                    {
                        clusters = kmeans.Learn(dataVectors);
                        currentClusterLabels = clusters.Decide(dataVectors);
                    }
                    catch (Exception kmeansEx)
                    {
                        throw new Exception($"K-Means thất bại: {kmeansEx.Message}", kmeansEx);
                    }

                    if (currentClusterLabels == null || clusters == null || clusters.Centroids == null)
                    {
                        throw new Exception("K-Means không tạo ra kết quả hợp lệ (nhãn hoặc tâm cụm).");
                    }

                    int distinctLabelsFound = currentClusterLabels.Distinct().Count();
                    if (distinctLabelsFound == 0) { throw new Exception("K-Means không tìm thấy cụm riêng biệt nào."); }
                    if (distinctLabelsFound < 2) { throw new Exception($"K-Means chỉ tìm thấy {distinctLabelsFound} cụm riêng biệt. LDA yêu cầu ít nhất 2."); }
                    if (ldaComponents >= distinctLabelsFound) { throw new Exception($"Số lượng thành phần LDA (d={ldaComponents}) phải nhỏ hơn số lượng cụm riêng biệt tìm thấy ({distinctLabelsFound}). Vui lòng giảm 'Thành phần LDA' hoặc kiểm tra kết quả K-Means."); }


                    UpdateStatus($"Đang thực hiện LDA (d={ldaComponents})...", 60);
                    LinearDiscriminantAnalysis currentLda = new LinearDiscriminantAnalysis() { NumberOfOutputs = ldaComponents };
                    try
                    {
                        currentLda.Learn(dataVectors, currentClusterLabels);
                    }
                    catch (Exception ldaEx)
                    {
                        throw new Exception($"Học LDA thất bại: {ldaEx.Message}...", ldaEx);
                    }

                    UpdateStatus("Đang chiếu các vector khối bằng LDA...", 75);
                    // double[][] currentProjectedVectors = currentLda.Transform(dataVectors);


                    UpdateStatus("Đang tái tạo từ các tâm cụm K-Means...", 90);
                    double[][] reconstructedVectorsFromCentroids = new double[dataVectors.Length][];
                    for (int i = 0; i < dataVectors.Length; i++)
                    {
                        int label = currentClusterLabels[i];
                        if (label >= 0 && label < clusters.Centroids.Length && clusters.Centroids[label] != null && clusters.Centroids[label].Length == vectorDim)
                        {
                            reconstructedVectorsFromCentroids[i] = (double[])clusters.Centroids[label].Clone();
                        }
                        else
                        {
                            Debug.WriteLine($"Cảnh báo: Nhãn ({label}) hoặc dữ liệu tâm cụm không hợp lệ cho khối {i}. Sử dụng vector mặc định (đen).");
                            reconstructedVectorsFromCentroids[i] = new double[vectorDim];
                        }
                    }

                    Bitmap rebuiltBitmap = RebuildImageFromBlocks(reconstructedVectorsFromCentroids,
                                                                grayscaleBitmap.Width,
                                                                grayscaleBitmap.Height,
                                                                blockSize);
                    UpdateStatus("Hoàn tất tái tạo từ tâm cụm.", 95);
                    return rebuiltBitmap;

                });

                stopwatch.Stop();
                if (reconstructedImage != null)
                {
                    pictureBoxReconstructed.Image = reconstructedImage;
                    string statusMsg = $"Hoàn thành (Tái tạo từ tâm cụm K-Means) trong {stopwatch.Elapsed.TotalSeconds:F2} giây.";


                    if (!string.IsNullOrEmpty(originalFilePath))
                    {
                        try
                        {
                            //string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                            //Directory.CreateDirectory(outputDirectory);

                            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                            DirectoryInfo binDirInfo = Directory.GetParent(baseDirectory);
                            DirectoryInfo projectDirInfo = binDirInfo?.Parent;
                            DirectoryInfo parentOfProjectDirInfo = projectDirInfo?.Parent;

                            string outputDirectory;
                            if (parentOfProjectDirInfo != null && parentOfProjectDirInfo.Exists)
                            {
                                outputDirectory = Path.Combine(parentOfProjectDirInfo.FullName, "output");
                            }
                            else if (projectDirInfo != null && projectDirInfo.Exists)
                            {
                                outputDirectory = Path.Combine(projectDirInfo.FullName, "output");
                                Debug.WriteLine("Cảnh báo: Không thể xác định thư mục cha của thư mục dự án. Lưu vào thư mục dự án thay thế.");
                            }
                            else
                            {
                                outputDirectory = Path.Combine(baseDirectory, "output");
                                Debug.WriteLine("Cảnh báo: Không thể xác định thư mục dự án hoặc thư mục cha. Lưu vào thư mục gốc thay thế.");
                            }

                            Directory.CreateDirectory(outputDirectory);
                            string originalFileName = Path.GetFileNameWithoutExtension(originalFilePath);
                            string outputFileName = $"{originalFileName}_reconstructed_k{k}_b{blockSize}.png";
                            string outputFilePath = Path.Combine(outputDirectory, outputFileName);
                            reconstructedImage.Save(outputFilePath, ImageFormat.Png);
                            statusMsg += $" | Đã lưu: output\\{outputFileName}";
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Lưu ảnh tái tạo thất bại:\n{ex.Message}", "Lỗi lưu tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            statusMsg += $" | Lưu thất bại!";
                        }
                    }
                    else
                    {
                        statusMsg += " | Bỏ qua lưu (không có đường dẫn gốc)";
                        Debug.WriteLine("Đường dẫn tệp gốc không có sẵn, không thể tự động lưu ảnh tái tạo.");
                    }
                    lblStatus.Text = statusMsg;

                }
                else
                {
                    lblStatus.Text = $"Xử lý hoàn tất (đã áp dụng LDA, tái tạo thất bại/bỏ qua) trong {stopwatch.Elapsed.TotalSeconds:F2} giây.";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                string errorMessage = ex.Message;
                Exception inner = ex.InnerException;
                while (inner != null)
                {
                    errorMessage += $"\n -> {inner.Message}";
                    inner = inner.InnerException;
                }
                MessageBox.Show($"Lỗi trong quá trình xử lý: {errorMessage}\n\nDấu vết ngăn xếp (trong cùng):\n{ex.StackTrace}", "Lỗi xử lý", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Xử lý thất bại.";
                pictureBoxReconstructed.Image = null;
            }
            finally
            {
                SetUIEnabled(true);
                progressBar.Visible = false;
            }
        }
        private void SetUIEnabled(bool enabled)
        {
            btnLoad.Enabled = enabled;
            btnProcess.Enabled = enabled;
            numBlockSize.Enabled = enabled;
            numClustersK.Enabled = enabled;
            numLdaComponents.Enabled = enabled;
        }

        private Bitmap ConvertToGrayscale(Bitmap original)
        {
            Bitmap gray = new Bitmap(original.Width, original.Height, PixelFormat.Format8bppIndexed);
            ColorPalette pal = gray.Palette;
            for (int i = 0; i < 256; i++)
                pal.Entries[i] = Color.FromArgb(i, i, i);
            gray.Palette = pal;

            BitmapData grayData = gray.LockBits(new Rectangle(0, 0, gray.Width, gray.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            BitmapData originalData = null;
            Bitmap temp32bpp = null;

            if (original.PixelFormat == PixelFormat.Format32bppArgb || original.PixelFormat == PixelFormat.Format32bppRgb || original.PixelFormat == PixelFormat.Format24bppRgb)
            {
                originalData = original.LockBits(new Rectangle(0, 0, original.Width, original.Height), ImageLockMode.ReadOnly, original.PixelFormat);
            }
            else
            {
                temp32bpp = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(temp32bpp))
                {
                    g.DrawImage(original, new Rectangle(0, 0, temp32bpp.Width, temp32bpp.Height));
                }
                originalData = temp32bpp.LockBits(new Rectangle(0, 0, temp32bpp.Width, temp32bpp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            }

            int heightInPixels = originalData.Height;
            int widthInPixels = originalData.Width;
            int grayStride = grayData.Stride;
            int origStride = originalData.Stride;
            int bytesPerPixelOrig = Image.GetPixelFormatSize(originalData.PixelFormat) / 8;

            unsafe
            {
                byte* PtrFirstPixelOrig = (byte*)originalData.Scan0;
                byte* PtrFirstPixelGray = (byte*)grayData.Scan0;
                Parallel.For(0, heightInPixels, y =>
                {
                    byte* currentLineOrig = PtrFirstPixelOrig + (y * origStride);
                    byte* currentLineGray = PtrFirstPixelGray + (y * grayStride);
                    for (int x = 0; x < widthInPixels; x++)
                    {
                        int byteOffset = x * bytesPerPixelOrig;
                        byte b = currentLineOrig[byteOffset];
                        byte g = currentLineOrig[byteOffset + 1];
                        byte r = currentLineOrig[byteOffset + 2];
                        byte grayValue = (byte)((0.299 * r) + (0.587 * g) + (0.114 * b));
                        currentLineGray[x] = grayValue;
                    }
                });
            }

            if (temp32bpp != null)
            {
                temp32bpp.UnlockBits(originalData);
                temp32bpp.Dispose();
            }
            else
            {
                original.UnlockBits(originalData);
            }
            gray.UnlockBits(grayData);

            return gray;
        }


        private List<double[]> ExtractAndVectorizeBlocks(Bitmap image, int blockSize)
        {
            if (image.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new ArgumentException("Lỗi nội bộ: Hình ảnh cung cấp cho ExtractAndVectorizeBlocks phải là ảnh xám 8bpp Indexed.");
            }
            List<double[]> vectors = new List<double[]>();
            int vectorLength = blockSize * blockSize;
            int blocksHigh = image.Height / blockSize;
            int blocksWide = image.Width / blockSize;
            if (blocksHigh == 0 || blocksWide == 0)
            {
                return vectors;
            }
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            int stride = imageData.Stride;
            unsafe
            {
                byte* scan0 = (byte*)imageData.Scan0;
                for (int blockRow = 0; blockRow < blocksHigh; blockRow++)
                {
                    int startY = blockRow * blockSize;
                    for (int blockCol = 0; blockCol < blocksWide; blockCol++)
                    {
                        int startX = blockCol * blockSize;
                        double[] vector = new double[vectorLength];
                        int k = 0;
                        for (int y = 0; y < blockSize; y++)
                        {
                            byte* row = scan0 + ((startY + y) * stride) + startX;
                            for (int x = 0; x < blockSize; x++)
                            {
                                vector[k++] = (double)row[x];
                            }
                        }
                        vectors.Add(vector);
                    }
                }
            }
            image.UnlockBits(imageData);
            return vectors;
        }


        private Bitmap RebuildImageFromBlocks(double[][] reconstructedVectors, int originalWidth, int originalHeight, int blockSize)
        {
            if (reconstructedVectors == null || reconstructedVectors.Length == 0)
            {
                throw new ArgumentNullException(nameof(reconstructedVectors), "Mảng vector đầu vào bị rỗng hoặc trống để tái tạo.");
            }

            Bitmap rebuiltImage = new Bitmap(originalWidth, originalHeight, PixelFormat.Format8bppIndexed);
            ColorPalette pal = rebuiltImage.Palette;
            for (int i = 0; i < 256; i++)
                pal.Entries[i] = Color.FromArgb(i, i, i);
            rebuiltImage.Palette = pal;

            BitmapData imageData = rebuiltImage.LockBits(new Rectangle(0, 0, originalWidth, originalHeight), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            int stride = imageData.Stride;
            int vectorLength = blockSize * blockSize;
            int blocksHigh = originalHeight / blockSize;
            int blocksWide = originalWidth / blockSize;
            int expectedTotalBlocks = blocksHigh * blocksWide;

            if (reconstructedVectors.Length != expectedTotalBlocks)
            {
                System.Diagnostics.Debug.WriteLine($"Cảnh báo trong RebuildImage: Số lượng vector ({reconstructedVectors.Length}) không khớp với số lượng khối dự kiến ({expectedTotalBlocks}). Hình ảnh có thể không hoàn chỉnh.");
            }

            int blockIndex = 0;
            unsafe
            {
                byte* scan0 = (byte*)imageData.Scan0;
                for (int blockRow = 0; blockRow < blocksHigh; blockRow++)
                {
                    int startY = blockRow * blockSize;
                    for (int blockCol = 0; blockCol < blocksWide; blockCol++)
                    {
                        int startX = blockCol * blockSize;
                        if (blockIndex >= reconstructedVectors.Length) break;
                        double[] vector = reconstructedVectors[blockIndex];
                        if (vector == null || vector.Length != vectorLength)
                        {
                            Debug.WriteLine($"Cảnh báo trong RebuildImage: Vector tại chỉ mục {blockIndex} bị rỗng hoặc có độ dài không chính xác ({(vector == null ? "null" : vector.Length.ToString())}, dự kiến {vectorLength}). Bỏ qua khối.");
                            blockIndex++;
                            continue;
                        }
                        int k = 0;
                        for (int y = 0; y < blockSize; y++)
                        {
                            byte* row = scan0 + ((startY + y) * stride) + startX;
                            for (int x = 0; x < blockSize; x++)
                            {
                                double val = vector[k++];
                                if (val < 0.0) val = 0.0;
                                else if (val > 255.0) val = 255.0;
                                row[x] = (byte)(val + 0.5);
                            }
                        }
                        blockIndex++;
                    }
                    if (blockIndex >= reconstructedVectors.Length) break;
                }
            }
            rebuiltImage.UnlockBits(imageData);
            return rebuiltImage;
        }

        private void UpdateStatus(string message, int progress)
        {
            if (!this.IsHandleCreated || this.IsDisposed) return;
            if (lblStatus.InvokeRequired)
            {
                lblStatus.BeginInvoke((MethodInvoker)delegate
                {
                    if (!lblStatus.IsDisposed) lblStatus.Text = message;
                });
            }
            else
            {
                if (!lblStatus.IsDisposed) lblStatus.Text = message;
            }
            if (progressBar.InvokeRequired)
            {
                progressBar.BeginInvoke((MethodInvoker)delegate
                {
                    if (!progressBar.IsDisposed) progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, progress)); ;
                });
            }
            else
            {
                if (!progressBar.IsDisposed) progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, progress));
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            originalBitmap?.Dispose();
            grayscaleBitmap?.Dispose();
            pictureBoxOriginal.Image?.Dispose();
            pictureBoxReconstructed.Image?.Dispose();
            base.OnFormClosing(e);
        }
    }
}