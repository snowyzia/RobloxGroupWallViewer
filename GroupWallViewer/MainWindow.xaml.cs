using GroupWallViewer.View.UserControls;
using Microsoft.Win32;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GroupWallViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void ClearCache(object sender, RoutedEventArgs e)
        {
            MessageBoxResult box = MessageBox.Show("Are you sure you want to clear the icon cache?", "Clear Icon Cache", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (box == MessageBoxResult.Yes)
            {
                string? directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                string userPicturesPath = Path.Combine(directoryPath, "UserPictures");
                Directory.CreateDirectory(userPicturesPath);

                string groupPicturesPath = Path.Combine(directoryPath, "GroupPictures");
                Directory.CreateDirectory(groupPicturesPath);

                foreach (var userPicture in Directory.EnumerateFiles(userPicturesPath))
                {
                    string extension = Path.GetExtension(userPicture);
                    if (extension == ".png")
                    {
                        File.Delete(userPicture);
                    }
                }

                foreach (var groupPicture in Directory.EnumerateFiles(groupPicturesPath))
                {
                    string extension = Path.GetExtension(groupPicture);
                    if (extension == ".png")
                    {
                        File.Delete(groupPicture);
                    }
                }
            }
        }
        private void CloseGroup(object sender, RoutedEventArgs e) // if we want to clear cache and stuff idk....
        {
            clearCache.Visibility = Visibility.Visible;
            infoText.Text = "Waiting on group data...";

            groupHolder.Children.Clear();
            groupName.Text = "";
            groupDescription.Text = "";
            groupIcon.Source = null;
        }
        private async void OpenJson(object sender, RoutedEventArgs e)
        {
            clearCache.Visibility = Visibility.Collapsed;

            OpenFileDialog groupDataDialog = new OpenFileDialog();
            groupDataDialog.Filter = "Group Wall Data| *.json";
            groupDataDialog.Title = "Please select group data...";
            groupDataDialog.RestoreDirectory = true;

            if (groupDataDialog.ShowDialog() == true)
            {
                try
                {
                    groupHolder.Children.Clear();
                    infoText.Text = "Group wall is loading, please wait!";

                    StreamReader groupDataReader = new StreamReader(groupDataDialog.FileName);
                    string groupDataJson = groupDataReader.ReadToEnd();

                    JsonDocument groupDataDocument = JsonDocument.Parse(groupDataJson);
                    var groupData = groupDataDocument.RootElement.GetProperty("data");

                    List<WallPost> wallPosts = new List<WallPost>();
                    foreach (JsonElement property in groupData.EnumerateArray())
                    {
                        var posterData = property.GetProperty("poster");
                        var userData = posterData.GetProperty("user");
                        var roleData = posterData.GetProperty("role");

                        var userId = userData.GetProperty("userId");
                        var username = userData.GetProperty("username");
                        var roleName = roleData.GetProperty("name");
                        var body = property.GetProperty("body");

                        WallPost wallPost = new WallPost();
                        wallPost.Username = username.ToString();
                        wallPost.WallText = body.ToString();
                        wallPost.AdditionalInformation = roleName.ToString();

                        BitmapImage userPicture = await GetUserPictureAsync(userId.ToString());
                        wallPost.playerIcon.Source = userPicture;
                        wallPosts.Add(wallPost);
                    }

                    // it takes a while and i dont want the user wondering why half the group wall is not loaded
                    foreach (WallPost wallPost in wallPosts)
                    {
                        groupHolder.Children.Add(wallPost);
                    }

                    OpenFileDialog groupInfoDialog = new OpenFileDialog();
                    groupInfoDialog.Filter = "Group Wall Info| *.json";
                    groupInfoDialog.Title = "Please select group info...";
                    groupInfoDialog.RestoreDirectory = true;

                    if (groupInfoDialog.ShowDialog() == true)
                    {
                        try
                        {
                            StreamReader groupInfoReader = new StreamReader(groupInfoDialog.FileName);
                            string json = groupInfoReader.ReadToEnd();

                            JsonDocument groupInfoDocument = JsonDocument.Parse(json);

                            var grpName = groupInfoDocument.RootElement.GetProperty("GroupName");
                            var grpDescription = groupInfoDocument.RootElement.GetProperty("Description");
                            var grpId = groupInfoDocument.RootElement.GetProperty("Id");
                            groupName.Text = grpName.ToString();
                            groupDescription.Text = grpDescription.ToString();

                            BitmapImage groupPicture = await GetGroupPictureAsync(grpId.ToString());
                            groupIcon.Source = groupPicture;
                            
                            infoText.Text = $"Fully loaded group: {grpName.ToString()}";
                        }
                        catch
                        {
                            infoText.Text = "Failed to load group information, group wall will still be shown.";
                        }
                    }
                }

                catch (Exception ex)
                {
                    infoText.Text = $"Group Data has failed to load: {ex.ToString()}";
                }
            }
        }

        private async Task<BitmapImage> GetUserPictureAsync(string userId)
        {
            string? directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string newPath = Path.Combine(directoryPath, "UserPictures");
            Directory.CreateDirectory(newPath);

            string imagePath = Path.Combine(newPath, $"{userId}.png");
            return await GetPictureAsync($"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId}&size=150x150&format=Png&isCircular=false", newPath, imagePath);
        }
        private async Task<BitmapImage> GetGroupPictureAsync(string groupId)
        {
            string? directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string newPath = Path.Combine(directoryPath, "GroupPictures");
            Directory.CreateDirectory(newPath);

            string imagePath = Path.Combine(newPath, $"{groupId}.png");
            return await GetPictureAsync($"https://thumbnails.roblox.com/v1/groups/icons?groupIds={groupId}&size=150x150&format=Png&isCircular=false", newPath, imagePath);
        }

        private async Task<BitmapImage> GetPictureAsync(string url, string newPath, string imagePath)
        {
            if (Path.Exists(imagePath))
            {
                return new BitmapImage(new Uri(imagePath));
            }
            else
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    var response = httpClient.GetStringAsync(url).Result;
                    JsonDocument userHeadshotInfo = JsonDocument.Parse(response.ToString());
                    var data = userHeadshotInfo.RootElement.GetProperty("data");
                    var imageUrl = data[0].GetProperty("imageUrl");

                    byte[] imageBytes = await httpClient.GetByteArrayAsync(imageUrl.ToString());
                    await File.WriteAllBytesAsync(imagePath, imageBytes); // automatically .png
                    return new BitmapImage(new Uri(imagePath));
                }
                catch
                {
                    return new BitmapImage(new Uri(Path.Combine(newPath, "1.png")));
                }
            }
        }
    }
}