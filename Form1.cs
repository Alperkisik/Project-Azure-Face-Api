using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections;

namespace face_api_form
{
    public partial class Form1 : Form
    {

        const string subscriptionKey = "XXXXX";
        const string uriBase =
            "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/detect";

        private const string faceEndpoint =
            "https://westcentralus.api.cognitive.microsoft.com";

        private readonly IFaceClient faceClient = new FaceClient(
            new ApiKeyServiceClientCredentials(subscriptionKey),
            new System.Net.Http.DelegatingHandler[] { });

        private IList<DetectedFace> faceList;

        private string[] faceDescriptions;

        string PersonGroupId = "";

        public Form1()
        {
            InitializeComponent();

            if (Uri.IsWellFormedUriString(faceEndpoint, UriKind.Absolute))
            {
                faceClient.Endpoint = faceEndpoint;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(faceEndpoint,
                    "Invalid URI");
                Environment.Exit(0);
            }

            txtGroup.Text = "grup";
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            // open file dialog   
            OpenFileDialog open = new OpenFileDialog();
            // image filters  
            open.Filter = "JPEG Image(*.jpg)|*.jpg";
            if (open.ShowDialog() == DialogResult.OK)
            {
                // display image in picture box  
                pictureBox1.Image = new Bitmap(open.FileName);
                // image file path  
                textBox1.Text = open.FileName;
                string filePath = open.FileName;

                faceList = await UploadAndDetectFaces(filePath);
                System.Windows.MessageBox.Show(String.Format(
                    "Detection Finished. {0} face(s) detected", faceList.Count));

                if (faceList.Count > 0)
                {
                    faceDescriptions = new String[faceList.Count];

                    for (int i = 0; i < faceList.Count; ++i)
                    {
                        DetectedFace face = faceList[i];

                        // Store the face description.
                        faceDescriptions[i] = FaceDescription(face);
                        FaceId = face.FaceId.ToString();
                    }
                    richTextBox1.Text = faceDescriptions[0];

                }
            }
        }

        string FaceId;

        private async Task<IList<DetectedFace>> UploadAndDetectFaces(string imageFilePath)
        {
            // The list of Face attributes to return.
            IList<FaceAttributeType> faceAttributes =
                new FaceAttributeType[]
                {
                    FaceAttributeType.Gender, FaceAttributeType.Age,
                    FaceAttributeType.Smile, FaceAttributeType.Emotion,
                    FaceAttributeType.Glasses, FaceAttributeType.Hair
                };

            // Call the Face API.
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    // The second argument specifies to return the faceId, while
                    // the third argument specifies not to return face landmarks.
                    IList<DetectedFace> faceList =
                        await faceClient.Face.DetectWithStreamAsync(
                            imageFileStream, true, false, faceAttributes);
                    return faceList;
                }
            }
            // Catch and display Face API errors.
            catch (APIErrorException f)
            {
                System.Windows.Forms.MessageBox.Show(f.Message);
                return new List<DetectedFace>();
            }
            // Catch and display all other errors.
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "Error");
                return new List<DetectedFace>();
            }
        }

        private string FaceDescription(DetectedFace face)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Face: ");

            // Add the gender, age, and smile.
            sb.Append(face.FaceAttributes.Gender);
            sb.Append(", ");
            sb.Append(face.FaceAttributes.Age);
            sb.Append(", ");
            sb.Append(String.Format("smile {0:F1}%, ", face.FaceAttributes.Smile * 100));

            // Add the emotions. Display all emotions over 10%.
            sb.Append("Emotion: ");
            Emotion emotionScores = face.FaceAttributes.Emotion;
            if (emotionScores.Anger >= 0.1f) sb.Append(
                String.Format("anger {0:F1}%, ", emotionScores.Anger * 100));
            if (emotionScores.Contempt >= 0.1f) sb.Append(
                String.Format("contempt {0:F1}%, ", emotionScores.Contempt * 100));
            if (emotionScores.Disgust >= 0.1f) sb.Append(
                String.Format("disgust {0:F1}%, ", emotionScores.Disgust * 100));
            if (emotionScores.Fear >= 0.1f) sb.Append(
                String.Format("fear {0:F1}%, ", emotionScores.Fear * 100));
            if (emotionScores.Happiness >= 0.1f) sb.Append(
                String.Format("happiness {0:F1}%, ", emotionScores.Happiness * 100));
            if (emotionScores.Neutral >= 0.1f) sb.Append(
                String.Format("neutral {0:F1}%, ", emotionScores.Neutral * 100));
            if (emotionScores.Sadness >= 0.1f) sb.Append(
                String.Format("sadness {0:F1}%, ", emotionScores.Sadness * 100));
            if (emotionScores.Surprise >= 0.1f) sb.Append(
                String.Format("surprise {0:F1}%, ", emotionScores.Surprise * 100));

            // Add glasses.
            sb.Append(face.FaceAttributes.Glasses);
            sb.Append(", ");

            // Add hair.
            sb.Append("Hair: ");

            // Display baldness confidence if over 1%.
            if (face.FaceAttributes.Hair.Bald >= 0.01f)
                sb.Append(String.Format("bald {0:F1}% ", face.FaceAttributes.Hair.Bald * 100));

            // Display all hair color attributes over 10%.
            IList<HairColor> hairColors = face.FaceAttributes.Hair.HairColor;
            foreach (HairColor hairColor in hairColors)
            {
                if (hairColor.Confidence >= 0.1f)
                {
                    sb.Append(hairColor.Color.ToString());
                    sb.Append(String.Format(" {0:F1}% ", hairColor.Confidence * 100));
                }
            }

            // Return the built string.
            return sb.ToString();
        }

        private async void Button2_Click(object sender, EventArgs e)
        {
            PersonGroupId = txtGroup.Text;
            //Identify
            FaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey), new System.Net.Http.DelegatingHandler[] { })
            {
                Endpoint = faceEndpoint
            };

            string imageUrl = textBox1.Text;
            Stream s = File.OpenRead(imageUrl);

            IList<DetectedFace> foundFaces = await faceClient.Face.DetectWithStreamAsync(s, true);
            if (foundFaces.Count > 0)
            {
                var result = await faceClient.Face.IdentifyAsync(foundFaces.Select(x => x.FaceId.Value).ToList(), PersonGroupId, maxNumOfCandidatesReturned: 3);
                foreach (var identifyResult in result)
                {
                    if (identifyResult.Candidates.Count == 0)
                    {
                        System.Windows.Forms.MessageBox.Show("No result");
                    }
                    else
                    {
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        double percentage = identifyResult.Candidates[0].Confidence;
                        //database kısmında burada candidate id ile get async yapmak yerine local databaseden de bakılabilir
                        Person human = await faceClient.PersonGroupPerson.GetAsync(PersonGroupId, candidateId);

                        System.Windows.Forms.MessageBox.Show("Identified as " + human.Name + " percentage: " + percentage);
                    }
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Picture unacceptable");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private async void Button4_Click(object sender, EventArgs e)
        {
            PersonGroupId = txtGroup.Text;
            await faceClient.PersonGroup.CreateAsync(PersonGroupId, PersonGroupId);
            button4.Enabled = false;

        }

        private async void Button3_Click(object sender, EventArgs e)
        {
            //add person
            //CreatePersonResult friend1 = await faceClient.PersonGroupPerson.CreateAsync(PersonGroupId, "Anna");
            PersonGroupId = txtGroup.Text;

            PersonGroup humanGroup = await faceClient.PersonGroup.GetAsync(PersonGroupId);
            Person human = null;

            string personName = txtName.Text;
            human = await faceClient.PersonGroupPerson.CreateAsync(humanGroup.PersonGroupId, personName);

            string imageUrl = textBox1.Text;
            Stream s = File.OpenRead(imageUrl);

            PersistedFace face = await faceClient.PersonGroupPerson.AddFaceFromStreamAsync(humanGroup.PersonGroupId, human.PersonId, s);

            //PersistedFace face = await faceClient.PersonGroupPerson.AddFaceFromUrlAsync(humanGroup.PersonGroupId, human.PersonId, imageUrl);

            //PersistedFace face = await faceClient.PersonGroupPerson.AddFaceFromUrlAsync(humanGroup.PersonGroupId, human.PersonId, imageUrl);

            //await faceClient.PersonGroupPerson.AddFaceFromUrlAsync(
            //PersonGroupId, human.PersonId, imageUrl);

            await faceClient.PersonGroup.TrainAsync(PersonGroupId);
            listBox1.Items.Add(personName);

            System.Windows.Forms.MessageBox.Show("person added");
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            webcam = new FilterInfoCollection(FilterCategory.VideoInputDevice);//webcam dizisine mevcut kameraları dolduruyoruz.
            foreach (FilterInfo videocapturedevice in webcam)
            {
                comboBox1.Items.Add(videocapturedevice.Name);//kameraları combobox a dolduruyoruz.
            }
            comboBox1.SelectedIndex = 0;
        }
        private FilterInfoCollection webcam;
        private VideoCaptureDevice cam;

        bool run = false;
        private void Button5_Click(object sender, EventArgs e)
        {
            if (run == false)
            {
                cam = new VideoCaptureDevice(webcam[comboBox1.SelectedIndex].MonikerString);
                cam.NewFrame += new NewFrameEventHandler(cam_NewFrame);
                cam.Start();
                timer1.Start();
                btnCamera.Text = "stop";
                run = true;
            }
            else
            {
                cam.Stop();
                timer1.Stop();
                btnCamera.Text = "start";
            }
        }

        private void cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bit = (Bitmap)eventArgs.Frame.Clone();
            pictureBox2.Image = bit;
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            if (cam.IsRunning) //kamera açıksa kapatıyoruz.
            {
                cam.Stop();
                timer1.Stop();
            }
        }

        private async void Timer1_Tick(object sender, EventArgs e)
        {

            using (Stream imgFile = ImageToStream(pictureBox2.Image,
                           System.Drawing.Imaging.ImageFormat.Jpeg))
            {
                faceList = await UploadAndDetectFacesWithStream(imgFile);

                if (faceList.Count > 0)
                {
                    System.Windows.MessageBox.Show(String.Format(
                    "Detection Finished. {0} face(s) detected", faceList.Count));

                    faceDescriptions = new String[faceList.Count];

                    for (int i = 0; i < faceList.Count; ++i)
                    {
                        DetectedFace face = faceList[i];

                        // Store the face description.
                        faceDescriptions[i] = FaceDescription(face);
                        FaceId = face.FaceId.ToString();
                    }
                    richTextBox2.Text = faceDescriptions[0];


                    #region Identify
                    //identify
                    PersonGroupId = txtGroup.Text;
                    var result = await faceClient.Face.IdentifyAsync(faceList.Select(x => x.FaceId.Value).ToList(), PersonGroupId, maxNumOfCandidatesReturned: 3);
                    foreach (var identifyResult in result)
                    {
                        if (identifyResult.Candidates.Count == 0)
                        {
                            System.Windows.Forms.MessageBox.Show("Unknown");
                        }
                        else
                        {
                            var candidateId = identifyResult.Candidates[0].PersonId;
                            double percentage = identifyResult.Candidates[0].Confidence;
                            //database kısmında burada candidate id ile get async yapmak yerine local databaseden de bakılabilir
                            Person human = await faceClient.PersonGroupPerson.GetAsync(PersonGroupId, candidateId);

                            System.Windows.Forms.MessageBox.Show("Identified as " + human.Name + " percentage: " + percentage);
                        }
                    }
                    #endregion 
                }
            }
        }

        private async Task<IList<DetectedFace>> UploadAndDetectFacesWithStream(Stream imageFile)
        {
            // The list of Face attributes to return.
            IList<FaceAttributeType> faceAttributes =
                new FaceAttributeType[]
                {
                    FaceAttributeType.Gender, FaceAttributeType.Age,
                    FaceAttributeType.Smile, FaceAttributeType.Emotion,
                    FaceAttributeType.Glasses, FaceAttributeType.Hair
                };

            // Call the Face API.
            try
            {
                IList<DetectedFace> faceList =
                        await faceClient.Face.DetectWithStreamAsync(
                            imageFile, true, false, faceAttributes);

                timer1.Stop();
                cam.Stop();
                btnCamera.Text = "start";
                run = false;

                return faceList;
            }
            // Catch and display Face API errors.
            catch (APIErrorException f)
            {
                // System.Windows.Forms.MessageBox.Show(f.Message);
                return new List<DetectedFace>();
            }
            // Catch and display all other errors.
            catch (Exception e)
            {
                //System.Windows.Forms.MessageBox.Show(e.Message, "Error");
                return new List<DetectedFace>();
            }
        }

        public Stream ImageToStream(Image image, System.Drawing.Imaging.ImageFormat format)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, format);
            ms.Position = 0;
            return ms;
        }

        private async void Button5_Click_1(object sender, EventArgs e)
        {
            /*timer1.Stop();
            cam.Stop();
            run = false;
            btnCamera.Text = "stop";*/

            PersonGroupId = txtGroup.Text;

            PersonGroup humanGroup = await faceClient.PersonGroup.GetAsync(PersonGroupId);
            Person human = null;

            string personName = textBox2.Text;
            human = await faceClient.PersonGroupPerson.CreateAsync(humanGroup.PersonGroupId, personName);

            using (Stream imgFile = ImageToStream(pictureBox2.Image,
                           System.Drawing.Imaging.ImageFormat.Jpeg))
            {
                PersistedFace face = await faceClient.PersonGroupPerson.AddFaceFromStreamAsync(humanGroup.PersonGroupId, human.PersonId, imgFile);

                await faceClient.PersonGroup.TrainAsync(PersonGroupId);

                listBox1.Items.Add(personName);

                System.Windows.Forms.MessageBox.Show("person added");
            }


        }
    }
}
