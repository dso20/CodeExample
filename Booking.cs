using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
using System.Web;

namespace ParaConfs.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int? BookingNumber { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; } 
        public string Email { get; set; }
        public string Phone { get; set; }

        //[RegularExpression(@"^.*(?=.*[!@#$%^&*\(\)_\-+=]).*$")]
        //[DataType(DataType.Password)]
        //public string Password { get; set; }
        public DateTime? DOB { get; set; }
        public DateTime? DateConfirmed { get; set; }
        public DateTime? DateChoice1 { get; set; }
        public DateTime? DateChoice2 { get; set; }
        public string PFPUrl { get; set; }
        public int? HeightFeet { get; set; }
        public int? HeightInches { get; set; }
        public int? WeightStone { get; set; }
        public int? WeightPounds { get; set; }


        [Display(Name = "Full Cost")]
        public decimal PromoterPrice { get; set; } //this should probs represent the amount to raise on the page//in general. Which will be different for bookings paying JC
        [Display(Name = "Sponsorship Raised")]
        public decimal SPTarget { get; set; } //on page// so amount raised
        public decimal SPActual { get; set; } //charity lines
        public decimal Withheld { get; set; }
        public decimal GAAmount { get; set; }
        public decimal JCRaised { get; set; } //Event payment lines
        public decimal JumpCost { get; set; } //this might be able to go under venue, leave out till decide
        public bool TermsSigned{ get; set; }
        public bool GV { get; set; }
        public bool SelfFund { get; set; }
        public bool InvoiceSkyline { get; set; }
        public bool JCCovered { get; set; }
        public string Sex { get; set; }

        public Venue Venue { get; set; }
        public int VenueId { get; set; }
        public Charity Charity { get; set; }
        public int CharityId { get; set; }
        public VenueWeightLimit VenueWeightLimit { get; set; }
        public MultipleBooking MultipleBooking { get; set; }
        public PFPBooking PFPBooking { get; set; }

        public string JumpType { get; set; }
        public string VenueChange { get; set; }
        public DateTime? DateChange { get; set; }

        public string JCFundMethod { get; set; }

        public DateTime? DateTimeCreated { get; set; }
        public DateTime? DateTimeModified { get; set; }

        // BOOKING MAGIC STRINGS HERE - TO MAKE THEM LESS MAGICAL
        public const string JumpTypeTandem = "TandemSkydive";
        public const string JumpTypeStaticSquare = "StaticSquare";
        public const string JumpTypeAFF = "AcceleratedFreefall";
        public const string SessionBookingId = "BookingId";

        public const string JCFundSelf = "SelfFund";
        public const string JCFundDonations = "DonationFund";


    }

}