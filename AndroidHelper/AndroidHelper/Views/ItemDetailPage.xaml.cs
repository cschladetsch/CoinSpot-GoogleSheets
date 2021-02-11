using AndroidHelper.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace AndroidHelper.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}