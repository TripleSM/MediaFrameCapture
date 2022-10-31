// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUI3MediaFrameCapture
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            //this.StartPreviewAsync();
        }

        Windows.Media.Capture.MediaCapture mediaCaptureManager;
        MediaFrameReader mediaFrameReader;
        private bool captureManagerInitialized = false;

        Image imagePreviewElement;

        private SoftwareBitmap backBuffer;
        private bool taskFrameRenderRunning = false;

        private async void StartPreviewAsync()
        {
            try
            {
                if (captureManagerInitialized == true)
                {
                    return;
                }

                //1. Select frame sources and frame source groups//
                var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
                if (frameSourceGroups.Count <= 0)
                {
                    TxtActivityLog.Text = "No source groups found.";
                    return;
                }

                //Get the first frame source group and frame source, Or write your code to select them//
                MediaFrameSourceGroup selectedGroup = frameSourceGroups[0]; 
                MediaFrameSourceInfo frameSourceInfo = selectedGroup.SourceInfos[0];

                //2. Initialize the MediaCapture object to use the selected frame source group//
                mediaCaptureManager = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings
                {
                    SourceGroup = selectedGroup,
                    SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu
                };

                await mediaCaptureManager.InitializeAsync(settings);

                //3. Initialize Image Preview Element with xaml Image Element.//
                imagePreviewElement = imagePreview;
                imagePreviewElement.Source = new SoftwareBitmapSource();

                //4. Create a frame reader for the frame source//
                MediaFrameSource mediaFrameSource = mediaCaptureManager.FrameSources[frameSourceInfo.Id];
                mediaFrameReader = await mediaCaptureManager.CreateFrameReaderAsync(mediaFrameSource, MediaEncodingSubtypes.Argb32);
                mediaFrameReader.FrameArrived += FrameReader_FrameArrived;
                await mediaFrameReader.StartAsync();

                captureManagerInitialized = true;
                TxtActivityLog.Text = "Media preview from device: " + selectedGroup.DisplayName;

            }
            catch (Exception Exc)
            {
                TxtActivityLog.Text = "MediaCapture initialization failed: " + Exc.Message;
            }
        }

        private async Task CleanupMediaCaptureAsync()
        {
            if (mediaCaptureManager != null)
            {
                using (var mediaCapture = mediaCaptureManager)
                {
                    mediaCaptureManager = null;

                    mediaFrameReader.FrameArrived -= FrameReader_FrameArrived;
                    await mediaFrameReader.StopAsync();
                    mediaFrameReader.Dispose();
                }
            }

            captureManagerInitialized = false;
            TxtActivityLog.Text = "Media preview has canceled.";
        }

        private void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

            if (softwareBitmap != null)
            {
                if (softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                // Swap the processed frame to _backBuffer and dispose of the unused image.
                softwareBitmap = Interlocked.Exchange(ref backBuffer, softwareBitmap);
                softwareBitmap?.Dispose();

                // Changes to XAML ImageElement must happen on UI thread through Dispatcher
                //var task = imagePreviewElement.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                _ = imagePreviewElement.DispatcherQueue.TryEnqueue(async () =>
                {
                    // Don't let two copies of this task run at the same time.
                    if (taskFrameRenderRunning)
                    {
                        return;
                    }
                    taskFrameRenderRunning = true;

                    // Keep draining frames from the backbuffer until the backbuffer is empty.
                    SoftwareBitmap latestBitmap;
                    while ((latestBitmap = Interlocked.Exchange(ref backBuffer, null)) != null)
                    {
                        var imageSource = (SoftwareBitmapSource)imagePreviewElement.Source;
                        await imageSource.SetBitmapAsync(latestBitmap);
                        latestBitmap.Dispose();
                    }

                    taskFrameRenderRunning = false;
                });
            }

            //mediaFrameReference.Dispose();
        }

        async private void InitCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //captureManager = new MediaCapture();
                //await captureManager.InitializeAsync();
                //TxtActivityLog.Text = "Camera has Initialized.";

                throw new NotImplementedException();
            }
            catch (Exception Exc)
            {
                TxtActivityLog.Text = Exc.Message;
            }
        }

        async private void StartCapturePreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //capturePreview.Source = captureManager;
                //await captureManager.StartPreviewAsync();   
                //TxtActivityLog.Text = "Media Preview has started.";

                this.StartPreviewAsync();
            }
            catch (Exception Exc)
            {
                TxtActivityLog.Text = Exc.Message;
            }
        }

        async private void StopCapturePreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //await captureManager.StopPreviewAsync();
                //TxtActivityLog.Text = "Media Preview has canceled.";

                await CleanupMediaCaptureAsync();
            }
            catch (Exception Exc)
            {
                TxtActivityLog.Text = Exc.Message;
            }
        }

        async private void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            if (captureManagerInitialized == false)
            {
                return;
            }

            try
            {
                ImageEncodingProperties imgFormat = ImageEncodingProperties.CreateJpeg();

                // create storage file in local app storage
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("TestPhoto.jpg",
                    CreationCollisionOption.GenerateUniqueName);

                // take photo
                await mediaCaptureManager.CapturePhotoToStorageFileAsync(imgFormat, file);

                // Get photo as a BitmapImage
                BitmapImage bmpImage = new BitmapImage(new Uri(file.Path));

                // imagePreview is a <Image> object defined in XAML
                imageCapture.Source = bmpImage;
            }
            catch (Exception Exc)
            {
                TxtActivityLog.Text = Exc.Message;
            }
        }

        //private void myButton_Click(object sender, RoutedEventArgs e)
        //{
        //    myButton.Content = "Clicked";
        //}
    }
}
