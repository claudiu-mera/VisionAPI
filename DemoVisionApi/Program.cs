using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DemoVisionApi
{
    class Program
    {
        const string skey = "d5fbe845106848d083676c92e96b1e8a";

        const string uriBase = "https://westeurope.api.cognitive.microsoft.com/vision/v1.0/";

        static void Main(string[] args)
        {
            string imageFilePath = @"E:\pictures\Elvetia 2018\IMG_1256.jpg";
            string imageWithTextFilePath = @"E:\pictures\vision\receipt_preview.jpg";
            string imageWithHandwritingPath = @"E:\pictures\vision\handwriting.png";

            //AnalyzeImage(imageFilePath);

            //GenerateThumbnail(imageFilePath, 80, 80, true);

            //SmartThumbnail(imageFilePath, 120, 120, true);

            //SmartProcessingImageShowResults(imageFilePath, "analyze");
            //SmartProcessingImageShowResults(imageFilePath, "describe");
            //SmartProcessingImageShowResults(imageFilePath, "tag");

            //TextExtraction(imageWithTextFilePath, false );
            HandwritingExtraction(imageWithHandwritingPath, false);

            Console.ReadLine();
        }

        public static void HandwritingExtraction(string fileName, bool wrds)
        {
            Task.Run(async () => {
                string[] results = await HandwritingExtractionCore(fileName, wrds);
                PrintResults(results);
            }).Wait();
        }

        public static void TextExtraction(string fileName, bool wrds)
        {
            Task.Run(async () => {
                string[] results = await TextExtractionCore(fileName, wrds);
                PrintResults(results);
            }).Wait();
        }

        private static void PrintResults(string[] results)
        {
            foreach(string result in results)
            {
                Console.WriteLine(result);
            }
        }

        private static async Task<string[]> HandwritingExtractionCore(string fileName, bool wrds)
        {
            VisionServiceClient client = new VisionServiceClient(skey, uriBase);
            string[] textResults = null;

            if (File.Exists(fileName))
            {
                using (Stream stream = File.OpenRead(fileName))
                {
                    HandwritingRecognitionOperation op = 
                        await client.CreateHandwritingRecognitionOperationAsync(stream );
                    HandwritingRecognitionOperationResult res = 
                        await client.GetHandwritingRecognitionOperationResultAsync(op);

                    textResults = GetExtractedHandwriting(res, wrds);
                }
            }

            return textResults;
        }

        private static async Task<string[]> TextExtractionCore(string fileName, bool wrds)
        {
            VisionServiceClient client = new VisionServiceClient(skey, uriBase);
            string[] textResults = null;

            if(File.Exists(fileName))
            {
                using (Stream stream = File.OpenRead(fileName))
                {
                    OcrResults res = await client.RecognizeTextAsync(stream, "unk", false);
                    textResults = GetExtracted(res, wrds);
                }                   
            }

            return textResults;
        }

        private static string[] GetExtractedHandwriting(HandwritingRecognitionOperationResult res, bool wrds)
        {
            List<string> items = new List<string>();

            foreach (HandwritingTextLine l in res.RecognitionResult.Lines)
            {
                if (wrds)
                {
                    items.AddRange(GetWordsHandwriting(l));
                }
                else
                {
                    items.Add(GetLineAsStringHandwriting(l));
                }
            }

            return items.ToArray();
        }

        private static string[] GetExtracted(OcrResults res, bool wrds)
        {
            List<string> items = new List<string>();

            foreach(Region r in res.Regions)
            {
                foreach(Line l in r.Lines)
                {
                    if(wrds)
                    {
                        items.AddRange(GetWords(l));
                    }
                    else
                    {
                        items.Add(GetLineAsString(l));
                    } 
                }
            }

            return items.ToArray();
        }

        private static List<string> GetWords(Line line)
        {
            List<string> words = new List<string>();

            foreach(Word w in line.Words)
            {
                words.Add(w.Text);
            }

            return words;
        }

        private static string GetLineAsString(Line l)
        {
            List<string> words = GetWords(l);
            return words.Count > 0 ? string.Join(" ", words) : string.Empty;
        }

        private static List<string> GetWordsHandwriting(HandwritingTextLine line)
        {
            List<string> words = new List<string>();

            foreach (HandwritingTextWord w in line.Words)
            {
                words.Add(w.Text);
            }

            return words;
        }

        private static string GetLineAsStringHandwriting(HandwritingTextLine l)
        {
            List<string> words = GetWordsHandwriting(l);
            return words.Count > 0 ? string.Join(" ", words) : string.Empty;
        }

        public static async void SmartProcessingImageShowResults(string imageFilePath, string method)
        {
            Task.Run(async () => {
                string imgName = Path.GetFileName(imageFilePath);
                Console.WriteLine($"Checking image {imgName}");

                AnalysisResult analyzed = await SmartImageProcessing(imageFilePath, method);

                switch(method)
                {
                    case "analyze":
                        ShowResults(analyzed, analyzed.Categories, "Categories");
                        ShowFaces(analyzed);
                        break;
                    case "describe":
                        ShowCaptions(analyzed);
                        break;
                    case "tag":
                        ShowTags(analyzed, 0.9);
                        break;
                }
            }).Wait();
        }

        public static async Task<AnalysisResult> SmartImageProcessing(string imageFilePath, string method)
        {
            AnalysisResult analyzed = null;
            VisionServiceClient client = new VisionServiceClient(skey, uriBase);

            IEnumerable<VisualFeature> visualFeatures = GetVisualFeatures();

            if(File.Exists(imageFilePath))
            {
                using (FileStream stream = File.OpenRead(imageFilePath))
                {
                    switch(method.ToLower())
                    {
                        case "analyze":
                            analyzed =  await client.AnalyzeImageAsync(stream, visualFeatures);
                            break;
                        case "describe":
                            analyzed = await client.DescribeAsync(stream);
                            break;
                        case "tag":
                            analyzed = await client.GetTagsAsync(stream);
                            break;
                    }   
                }
            }

            return analyzed;
        }

        private static IEnumerable<VisualFeature> GetVisualFeatures()
        {
            return new VisualFeature[]
            {
                VisualFeature.Adult,
                VisualFeature.Categories,
                VisualFeature.Color,
                VisualFeature.Description,
                VisualFeature.Faces,
                VisualFeature.ImageType,
                VisualFeature.Tags
            };
        }

        public static void SmartThumbnail(string imageFilePath, int width, int height, bool smart)
        {
            Task.Run(async() => {
                string imgName = Path.GetFileName(imageFilePath);
                Console.WriteLine($"Thumbnail image for {imgName}");

                byte[] thumbnail = await SmartThumbnailGeneration(imageFilePath, width, height, smart);

                string thumbnailFullPath = $"{Path.GetDirectoryName(imageFilePath)}\\" +
                                            $"thumbnail_{DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss")}.jpg";

                using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(thumbnailFullPath, FileMode.OpenOrCreate, FileAccess.Write)))
                {
                    binaryWriter.Write(thumbnail);
                }

                Console.WriteLine("Thumbnail generated");
            }).Wait();
        }

        private static async Task<byte[]> SmartThumbnailGeneration(string imageFilePath, int width, int height, bool smart)
        {
            byte[] thumbnail = null;

            VisionServiceClient client = new VisionServiceClient(skey, uriBase);

            if(File.Exists(imageFilePath))
            {
                using (Stream stream = File.OpenRead(imageFilePath))
                {
                    thumbnail = await client.GetThumbnailAsync(stream, width, height, smart);
                }
            }
            return thumbnail;
        }

        private static async void GenerateThumbnail(string imageFilePath, int width, int height, bool smart)
        {
            byte[] thumbnail = await GetThumbnail(imageFilePath, width, height, smart);

            string thumbnailFullPath = $"{Path.GetDirectoryName(imageFilePath)}\\" +
                $"thumbnail_{DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss")}.jpg";

            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(thumbnailFullPath, FileMode.OpenOrCreate, FileAccess.Write)))
            {
                binaryWriter.Write(thumbnail);
            }

            Console.WriteLine("Thumbnail generated");
        }

        private static async Task<byte[]> GetThumbnail(string imageFilePath, int width, int height, bool smart)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", skey);

            string requestParameters = $"width={width}&height={height}&smartCropping={smart}";
            string uri = uriBase + "generateThumbnail?" + requestParameters;

            byte[] imageBytes = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(imageBytes))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                HttpResponseMessage response = await client.PostAsync(uri, content);

                byte[] responseData = await response.Content.ReadAsByteArrayAsync();
                return responseData;
            }          
        }

        public static async void AnalyzeImage(string imageFilePath)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", skey);

            string requestParameters = "visualFeatures=Categories,Description,Color&language=en";
            string uri = uriBase + "analyze?" + requestParameters;

            HttpResponseMessage response = null;

            byte[] imageBytes = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(imageBytes))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);
                string contentString = await response.Content.ReadAsStringAsync();

                Console.WriteLine("Response:\n");
                Console.WriteLine(JsonPrettyPrint(contentString));
            }
        }

        private static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }


        private static void ShowTags(AnalysisResult analyzed, double confidenceLevel)
        {
            var tags = from tag in analyzed.Tags
                       where tag.Confidence > confidenceLevel
                       select tag.Name;

            if (tags.Count() > 0)
            {
                Console.WriteLine("Tags:");
                Console.WriteLine(string.Join(", ", tags));
            }
        }

        private static void ShowCaptions(AnalysisResult analyzed)
        {
            var captions = from caption in analyzed.Description.Captions
                           select caption.Text + "-" + caption.Confidence;

            if (captions.Count() > 0)
            {
                Console.WriteLine("Captions:");
                Console.WriteLine(string.Join(", ", captions));
            }
        }

        private static void ShowFaces(AnalysisResult analyzed)
        {
            var faces = from face in analyzed.Faces
                        select face.Gender + "-" + face.Age;

            if (faces.Count() > 0)
            {
                Console.WriteLine("Faces:");
                Console.WriteLine(string.Join(", ", faces));
            }
        }

        private static void ShowResults(AnalysisResult analyzed, NameScorePair[] nps, string resName)
        {
            var results = from result in nps select result.Name + "-" + result.Score.ToString();

            if (results.Any())
            {
                Console.WriteLine($"{resName}:");
                Console.WriteLine(string.Join(", ", results));
            }
        }

        private static string JsonPrettyPrint(string contentString)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(contentString);

            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }
    }
}
