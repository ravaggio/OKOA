using System.ComponentModel;
using Xamarin.Forms;

namespace ctf_final
{
    public class FormattedRating
    {
        private bool _expanded;

        public Rating RatingDetails { get; set; }
        public bool Expanded {
            get {
                return _expanded;
            }
            set {
                if (value != _expanded)
                {
                    _expanded = value;
                }
            }
        }
        public string StateIcon {
            get {
                if (_expanded)
                {
                    return "ic_arrow_up.png";
                }
                else
                {
                    return "ic_arrow_down.png";
                }
            }
        }
        public FormattedRating(Rating ratingDetails)
        {
            RatingDetails = ratingDetails;
            Expanded = false;
        }
        public Command TapCommand {
            get {
                return new Command(value => { Expanded ^= true; });
            }
        }
    }
}
