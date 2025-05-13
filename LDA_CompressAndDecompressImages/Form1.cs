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
using System.Diagnostics; // For Stopwatch
using System.IO; // <<< THÊM USING NÀY ĐỂ LÀM VIỆC VỚI FILE/FOLDER

// Accord.NET specific namespaces
// Make sure you have installed the following NuGet packages:
// Accord.MachineLearning, Accord.Statistics, Accord.Math, Accord.Imaging
//using Accord.Imaging; // For Image utilities like Blockmatching (though we implement manually here)
using Accord.MachineLearning; // For KMeans
using Accord.Statistics.Analysis; // For LDA
using Accord.Math; // For matrix/vector operations
using Accord.Math.Distances; // For KMeans distance

namespace ldaimage // Make sure this matches your project name
{
    public partial class Form1 : Form
    {
        Bitmap originalBitmap = null;
        Bitmap grayscaleBitmap = null;
        string originalFilePath = null; // <<< THÊM BIẾN LƯU ĐƯỜNG DẪN FILE GỐC

        // Store results between steps if needed (or process sequentially)
        // These member variables are currently not assigned back after processing,
        // but could be if needed for other operations.
        // List<double[]> blockVectors = null;
        // int[] clusterLabels = null;
        // LinearDiscriminantAnalysis lda = null;
        // double[][] projectedVectors = null;

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
            ofd.Title = "Select an Image File"; // Thêm tiêu đề cho rõ ràng

            // --- Phần sửa đổi ---
            try
            {
                // Lấy đường dẫn thư mục chạy ứng dụng (thường là bin/Debug hoặc bin/Release)
                string startupPath = AppDomain.CurrentDomain.BaseDirectory;
                // Kết hợp với tên thư mục "input"
                string inputDirPath = Path.Combine(startupPath, "input");

                // Kiểm tra xem thư mục "input" có tồn tại không
                if (Directory.Exists(inputDirPath))
                {
                    // Nếu tồn tại, đặt làm thư mục khởi đầu cho OpenFileDialog
                    ofd.InitialDirectory = inputDirPath;
                }
                else
                {
                    // Thông báo (tùy chọn) nếu không tìm thấy thư mục "input"
                    Console.WriteLine($"Warning: Input directory not found at '{inputDirPath}'. Using default.");
                    // Bạn có thể đặt một thư mục mặc định khác ở đây nếu muốn, ví dụ:
                    // ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có vấn đề khi lấy đường dẫn (ít khi xảy ra)
                Console.WriteLine($"Error determining initial directory: {ex.Message}");
            }
            // --- Hết phần sửa đổi ---

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Giải phóng tài nguyên cũ một cách an toàn
                    // Nên giải phóng Image của PictureBox trước khi giải phóng Bitmap gốc của nó
                    pictureBoxOriginal.Image?.Dispose();
                    pictureBoxReconstructed.Image?.Dispose();
                    originalBitmap?.Dispose();
                    grayscaleBitmap?.Dispose();

                    // Tải ảnh mới
                    // Sử dụng using để đảm bảo Bitmap tạm thời được giải phóng ngay cả khi có lỗi
                    using (Bitmap tempBitmap = new Bitmap(ofd.FileName))
                    {
                        // Tạo bản sao để tránh khóa file gốc
                        originalBitmap = new Bitmap(tempBitmap);
                    }

                    // Hiển thị ảnh gốc (tạo bản sao mới cho PictureBox để tránh xung đột Dispose)
                    pictureBoxOriginal.Image = new Bitmap(originalBitmap);

                    // Lưu đường dẫn file gốc
                    this.originalFilePath = ofd.FileName;

                    // Cập nhật trạng thái
                    lblStatus.Text = $"Loaded: {Path.GetFileName(ofd.FileName)}";

                    // Chuyển đổi sang ảnh xám
                    grayscaleBitmap = ConvertToGrayscale(originalBitmap); // Đảm bảo hàm này xử lý lỗi nếu có

                    // Xóa ảnh đã tái tạo cũ (nếu có)
                    pictureBoxReconstructed.Image = null; // Dispose đã được gọi ở trên

                    lblStatus.Text = "Image loaded. Ready to process.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading or processing image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    pictureBoxOriginal.Image?.Dispose();
                    pictureBoxReconstructed.Image?.Dispose();
                    originalBitmap?.Dispose();
                    grayscaleBitmap?.Dispose();

                    originalBitmap = null;
                    grayscaleBitmap = null;
                    pictureBoxOriginal.Image = null;
                    pictureBoxReconstructed.Image = null;
                    lblStatus.Text = "Failed to load image.";
                    this.originalFilePath = null;
                }
            }
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            if (grayscaleBitmap == null)
            {
                MessageBox.Show("Please load an image first.", "No Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int blockSize = (int)numBlockSize.Value;
            int k = (int)numClustersK.Value;
            int ldaComponents = (int)numLdaComponents.Value;

            if (ldaComponents >= k)
            {
                MessageBox.Show($"Number of LDA components (d={ldaComponents}) must be less than K (K={k}).\nPlease set d < K.",
                                "Invalid Parameters", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (blockSize <= 0 || blockSize > grayscaleBitmap.Width || blockSize > grayscaleBitmap.Height)
            {
                MessageBox.Show("Invalid Block size.", "Invalid Parameters", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetUIEnabled(false);
            progressBar.Visible = true;
            progressBar.Value = 0;
            lblStatus.Text = "Starting processing...";
            pictureBoxReconstructed.Image?.Dispose();
            pictureBoxReconstructed.Image = null;
            Stopwatch stopwatch = Stopwatch.StartNew();

            Bitmap reconstructedImage = null;

            try
            {
                reconstructedImage = await Task.Run<Bitmap>(() =>
                {
                    UpdateStatus("Extracting & Vectorizing Blocks...", 10);
                    List<double[]> currentBlockVectors = ExtractAndVectorizeBlocks(grayscaleBitmap, blockSize);
                    if (currentBlockVectors == null || currentBlockVectors.Count == 0) { throw new Exception("Could not extract blocks or no blocks found."); }
                    if (currentBlockVectors.Count < k) { throw new Exception($"Number of blocks ({currentBlockVectors.Count}) is less than K ({k}). Cannot perform K-Means."); }

                    double[][] dataVectors = currentBlockVectors.ToArray();
                    int vectorDim = dataVectors[0].Length;

                    UpdateStatus($"Running K-Means (K={k})...", 30);
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
                        throw new Exception($"K-Means failed: {kmeansEx.Message}", kmeansEx);
                    }

                    if (currentClusterLabels == null || clusters == null || clusters.Centroids == null)
                    {
                        throw new Exception("K-Means did not produce valid results (labels or centroids).");
                    }

                    int distinctLabelsFound = currentClusterLabels.Distinct().Count();
                    if (distinctLabelsFound == 0) { throw new Exception("K-Means found no distinct clusters."); }
                    if (distinctLabelsFound < 2) { throw new Exception($"K-Means found only {distinctLabelsFound} distinct cluster(s). LDA requires at least 2."); }
                    if (ldaComponents >= distinctLabelsFound) { throw new Exception($"Number of LDA components (d={ldaComponents}) must be less than the number of distinct clusters found ({distinctLabelsFound}). Please reduce 'LDA Components' or check K-Means results."); }


                    UpdateStatus($"Performing LDA (d={ldaComponents})...", 60);
                    LinearDiscriminantAnalysis currentLda = new LinearDiscriminantAnalysis() { NumberOfOutputs = ldaComponents };
                    try
                    {
                        currentLda.Learn(dataVectors, currentClusterLabels);
                    }
                    catch (Exception ldaEx)
                    {
                        throw new Exception($"LDA Learning failed: {ldaEx.Message}...", ldaEx);
                    }

                    UpdateStatus("Projecting block vectors using LDA...", 75);
                    // double[][] currentProjectedVectors = currentLda.Transform(dataVectors);


                    UpdateStatus("Reconstructing from K-Means centroids...", 90);
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
                            Debug.WriteLine($"Warning: Invalid label ({label}) or centroid data for block {i}. Using default (black) vector.");
                            reconstructedVectorsFromCentroids[i] = new double[vectorDim];
                        }
                    }

                    Bitmap rebuiltBitmap = RebuildImageFromBlocks(reconstructedVectorsFromCentroids,
                                                                grayscaleBitmap.Width,
                                                                grayscaleBitmap.Height,
                                                                blockSize);
                    UpdateStatus("Reconstruction from centroids complete.", 95);
                    return rebuiltBitmap;

                }); // End Task.Run

                stopwatch.Stop();

                // --- Hiển thị kết quả ---
                if (reconstructedImage != null)
                {
                    pictureBoxReconstructed.Image = reconstructedImage;
                    string statusMsg = $"Finished (Reconstructed from K-Means centroids) in {stopwatch.Elapsed.TotalSeconds:F2}s.";


                    if (!string.IsNullOrEmpty(originalFilePath)) // Chỉ lưu nếu có đường dẫn file gốc
                    {
                        try
                        {
                            // Tạo đường dẫn thư mục output (cùng cấp file exe)
                            //string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                            // Tạo thư mục nếu chưa có
                            //Directory.CreateDirectory(outputDirectory);

                            // --- Lấy đường dẫn thư mục cha của thư mục dự án (đi lên 3 cấp từ BaseDirectory) ---
                            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                            DirectoryInfo binDirInfo = Directory.GetParent(baseDirectory);
                            DirectoryInfo projectDirInfo = binDirInfo?.Parent;
                            DirectoryInfo parentOfProjectDirInfo = projectDirInfo?.Parent;

                            string outputDirectory; // Khai báo bên ngoài

                            // Ưu tiên lưu vào thư mục cha của thư mục dự án (thường là thư mục Solution)
                            if (parentOfProjectDirInfo != null && parentOfProjectDirInfo.Exists)
                            {
                                outputDirectory = Path.Combine(parentOfProjectDirInfo.FullName, "output");
                            }
                            // Nếu không được, thử lưu vào thư mục dự án
                            else if (projectDirInfo != null && projectDirInfo.Exists)
                            {
                                outputDirectory = Path.Combine(projectDirInfo.FullName, "output");
                                Debug.WriteLine("Warning: Could not determine parent of project directory. Saving to project directory instead.");
                            }
                            // Nếu vẫn không được, quay lại lưu vào thư mục base (bin\Debug)
                            else
                            {
                                outputDirectory = Path.Combine(baseDirectory, "output");
                                Debug.WriteLine("Warning: Could not determine project or parent directory. Saving to base directory instead.");
                            }

                            // Tạo thư mục output nếu nó chưa tồn tại
                            Directory.CreateDirectory(outputDirectory);

                            // Lấy tên file gốc không có phần mở rộng
                            string originalFileName = Path.GetFileNameWithoutExtension(originalFilePath);
                            // Tạo tên file mới (thêm tham số K và BlockSize)
                            string outputFileName = $"{originalFileName}_reconstructed_k{k}_b{blockSize}.png";
                            // Tạo đường dẫn file đầy đủ
                            string outputFilePath = Path.Combine(outputDirectory, outputFileName);

                            // Lưu ảnh với định dạng PNG
                            reconstructedImage.Save(outputFilePath, ImageFormat.Png);

                            // Cập nhật trạng thái bao gồm thông tin đã lưu
                            statusMsg += $" | Saved: output\\{outputFileName}";
                        }
                        catch (Exception ex)
                        {
                            // Thông báo lỗi nếu không lưu được
                            MessageBox.Show($"Failed to save reconstructed image:\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            statusMsg += $" | Save failed!";
                        }
                    }
                    else
                    {
                        statusMsg += " | Save skipped (no original path)";
                        Debug.WriteLine("Original file path not available, cannot save reconstructed image automatically.");
                    }
                    // Cập nhật label trạng thái cuối cùng
                    lblStatus.Text = statusMsg;

                }
                else
                {
                    lblStatus.Text = $"Processing finished (LDA applied, reconstruction failed/skipped) in {stopwatch.Elapsed.TotalSeconds:F2} seconds.";
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
                MessageBox.Show($"Error during processing: {errorMessage}\n\nStack Trace (innermost):\n{ex.StackTrace}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Processing failed.";
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
                temp32bpp.UnlockBits(originalData); // Unlock temp32bpp nếu nó được sử dụng
                temp32bpp.Dispose();
            }
            else
            {
                original.UnlockBits(originalData); // Unlock original nếu nó được sử dụng trực tiếp
            }
            gray.UnlockBits(grayData);

            return gray;
        }


        private List<double[]> ExtractAndVectorizeBlocks(Bitmap image, int blockSize)
        {
            if (image.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new ArgumentException("Internal Error: Image provided to ExtractAndVectorizeBlocks must be 8bpp Indexed grayscale.");
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
                throw new ArgumentNullException(nameof(reconstructedVectors), "Input vector array is null or empty for rebuilding.");
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
                System.Diagnostics.Debug.WriteLine($"Warning in RebuildImage: Vector count ({reconstructedVectors.Length}) doesn't match expected block count ({expectedTotalBlocks}). Image might be incomplete.");
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
                            Debug.WriteLine($"Warning in RebuildImage: Vector at index {blockIndex} is null or has incorrect length ({vector?.Length ?? -1}, expected {vectorLength}). Skipping block.");
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