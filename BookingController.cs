using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ParaConfs.Models;
using System.Data.Entity;
using System.Data.SqlTypes;
using System.Web.Routing;
using System.Web.WebPages;
using Microsoft.AspNet.Identity;
using ParaConfs.Utilities;
using ParaConfs.ViewModels;
using System.Security.Principal;
using Microsoft.Ajax.Utilities;
using System.Web;



namespace ParaConfs.Controllers
{
    public class BookingController : Controller
    {
        private ApplicationDbContext _context;

        public BookingController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Booking
        [Route("Booking/index/{id}/{dupe}")]
        [Route("Booking/index/{id}")]
        [UserTrackingFilter]
        public ActionResult Index(int id, bool dupe = false)
        {   
          //handle nulls, though not entirely sure needed LT as should be handeled by logging  in
            if ((int?)Session[Booking.SessionBookingId] == 0)
            { return RedirectToAction( "Index","Home"); }

            //if dupe booking need to set sessions
            if (dupe)
            {
                Session[Booking.SessionBookingId] = id;
                Session[Venue.SessionVenueId] = GetBooking.Booking("",id).VenueId;
            }

            //let's check that the current logged in user is looking at their profile
            //also acts as security so only admin//dupes can veiw multi profiles as it updates session when searching  
            var viewmodel = id != (int)Session[Booking.SessionBookingId] ? BookingViewModel((int)Session[Booking.SessionBookingId]) :  BookingViewModel(id);

            var userId = User.Identity.GetUserId();

            var userInDb = _context.Users.SingleOrDefault(u => u.Id == userId);
           
            if (userInDb != null)
            {
                if (userInDb.UniqueId.ToString() == Request.QueryString["uniqueId"])
                {
                    viewmodel.User = userInDb;
                }
            }
          
            //want to redirect to dupeview if has dupe email booking. Also want to check if we have returned from dupe view
            if (viewmodel.MultiId1 != 0 && dupe == false)
            {
                var viewmodel2 = new MultipleBookingsViewModel
                {
                    Leader = BookingViewModel(userInDb.BookingId), DupeBookingId = viewmodel.MultiId1
                };
                return View("MultipleBookings",viewmodel2);
            }
            

            return View(viewmodel);
        }



        [HttpPost]
        [UserTrackingFilter]
        public ActionResult Save(BookingIndexViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel = BookingViewModel(viewModel.ID);
                return View("Index", viewModel);
            }

            var bookingInDb = _context.Bookings.Single(b => b.Id == viewModel.ID);
            bookingInDb.DOB = viewModel.DobHidden != null ? viewModel.DobHidden.AsDateTime() : viewModel.DOB; //DobHidden is a string concatenated from DOB dropdowns, validated in DobValidation.cs
            bookingInDb.HeightFeet = viewModel.HeightFeet;
            bookingInDb.HeightInches = viewModel.HeightInches;
            bookingInDb.WeightStone = viewModel.WeightStone;
            bookingInDb.WeightPounds = viewModel.WeightPounds;
            bookingInDb.DateChoice1 = viewModel.DateChoice1;
            bookingInDb.DateChoice2 = viewModel.DateChoice2;
            bookingInDb.JCFundMethod = viewModel.JCFundMethod;
            bookingInDb.Sex = viewModel.Sex;
            //not DIFC pfp handled by STP so goes straight in to base to avoid some awkwardness. Let's ignore DIFC pages for now
            if (viewModel.PFPUrl != null)
            {   //bit of a mess, but updating the base then updating the paraconfs
                StoredProcedure.RunSTP("stpUpdateBaseSPPage", bookingInDb.Id, new Dictionary<string, string>() { { "SpPage", viewModel.PFPUrl } });
                if (bookingInDb.PFPUrl == null)
                { bookingInDb.PFPUrl = viewModel.PFPUrl; }
                bookingInDb.PFPUrl = bookingInDb.PFPUrl + "," + viewModel.PFPUrl;
            }

            bookingInDb.DateTimeModified = DateTime.Now;
            _context.SaveChanges();

            //let's try to update the base
            if (Request.Url.Host.ToString() != "localhost")
            { StoredProcedure.RunSTP("stpUpdateBase", bookingInDb.Id);}

            return RedirectToAction("Index", "Booking", new {Id = bookingInDb.Id });

        }


        public ViewResult Terms(int id)
        {
            var booking = _context.Bookings.SingleOrDefault(b => b.Id == id);
            var viewmodel = new TermsViewModel(booking);
          

            if (booking != null && booking.GV)
            { return View("TermsGV"); }

            return View(viewmodel);
        }



        [HttpPost]
        [UserTrackingFilter]
        public ActionResult Terms(TermsViewModel model)
        {
            var booking = _context.Bookings.SingleOrDefault(b => b.Id == model.ID);
            if (booking == null)
            {
                return HttpNotFound();
            }

            booking.TermsSigned = model.TermsSigned;
            _context.SaveChanges();

            var viewmodel = BookingViewModel(model.ID);
            
            return RedirectToAction("Index", "Booking", new { Id = model.ID });
        }




        [Route("Booking/FAQ")]
        [UserTrackingFilter]
        public ViewResult FAQ()
        {

            return View();
        }

        [Route("Booking/Forms")]
        [UserTrackingFilter]
        public ViewResult Forms(int id)
        {
            var viewmodel = BookingViewModel(id);
                
            return View(viewmodel);
        }

        [Route("Booking/Documents")]
        [UserTrackingFilter]
        public FilePathResult Download(string file)
        {
            return new FilePathResult(string.Format(@"~\Files\{0}", file), "text/plain");

        }


        [UserTrackingFilter]
        public ActionResult TermsAccept(int id)
        {

            var booking = _context.Bookings.SingleOrDefault(b => b.Id == id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            
            booking.TermsSigned = true;
            _context.SaveChanges();

            return RedirectToAction("Index", "Booking", new { Id = id });
        }

        [UserTrackingFilter]
        public ActionResult RequestVenueChange(FormCollection form)
        {

            int id = int.Parse(form["ID"].ToString());
            string newVenue = form["VenueChange"].ToString() == "" ? null : ((Venue.List)int.Parse(form["VenueChange"])).ToString();
            //DateTime? newDate = form["DateChange"] == "" ? new DateTime?() : Convert.ToDateTime(form["DateChange"]);

            var booking = _context.Bookings.Include(m => m.Venue).SingleOrDefault(b => b.Id == id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            
            booking.VenueChange = newVenue;
            //booking.DateChange = newDate == DateTime.MinValue ? null : newDate;

            //only save if different
            if (newVenue != booking.Venue.Name && newVenue != null)
            {
                _context.SaveChanges();
            }
            else
            {
                return RedirectToAction("Index", "Booking", new { Id = id });
            }


            //let's send an email to confirm
            var passdictionary = new Dictionary<string, string>()
            {
                {"newVenue", newVenue},
                {"oldVenue", booking.Venue.Name},
                {"BookingNumber", booking.BookingNumber.ToString() },
            };

            //lets send an email to the EC
            string subject = "Participant Venue Change Request - #" + booking.BookingNumber.ToString() + " - " +
                             booking.FirstName + " " + booking.Surname;

            var emailsend = new Message(new SendEmail(subject, "info@skylineparachuting.co.uk", ConfigurationManager.AppSettings["EmailFolder"] + ConfigurationManager.AppSettings["AirfieldChange"], false, passdictionary));
            emailsend.Send(null, String.IsNullOrEmpty(booking.Venue.ECEmail) ? ConfigurationManager.AppSettings["OngoingInbox"] : booking.Venue.ECEmail);

            return RedirectToAction("Index", "Booking", new { Id = id });
        }


        [UserTrackingFilter]
        public ActionResult RequestDateChange(FormCollection form)
        {

            int id = int.Parse(form["ID"].ToString());
            //string newVenue = form["VenueChange"].ToString() == "" ? null : ((Venue.List)int.Parse(form["VenueChange"])).ToString();
            DateTime? newDate = form["DateChange"] == "" ? new DateTime?() : Convert.ToDateTime(form["DateChange"]);

            var booking = _context.Bookings.Include(m => m.Venue).SingleOrDefault(b => b.Id == id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            
            //booking.VenueChange = newVenue;
            booking.DateChange = newDate == DateTime.MinValue ? null : newDate;

            //only save if different
            if (newDate != null && newDate != DateTime.MinValue)
            {
                _context.SaveChanges();
            }
            else
            {
                return RedirectToAction("Index", "Booking", new { Id = id });
            }


            //let's send an email to confirm
            var passdictionary = new Dictionary<string, string>()
            {
                {"BookingNumber", booking.BookingNumber.ToString() },
                {"oldDate", booking.DateConfirmed.ToString() },
                {"newDate", newDate.ToString() }
            };

            //lets send an email to the EC
            string subject = "Participant Date Change Request - #" + booking.BookingNumber.ToString() + " - " +
                             booking.FirstName + " " + booking.Surname;

            var emailsend = new Message(new SendEmail(subject, "info@skylineparachuting.co.uk", ConfigurationManager.AppSettings["EmailFolder"] + ConfigurationManager.AppSettings["DateChange"], false, passdictionary));
            emailsend.Send(null, String.IsNullOrEmpty(booking.Venue.ECEmail) ? ConfigurationManager.AppSettings["OngoingInbox"] : booking.Venue.ECEmail);

            return RedirectToAction("Index", "Booking", new { Id = id });
        }

        public ActionResult WeightChange(BookingIndexViewModel viewModel)
        {
            int newStone;
            int newPounds;

            if (int.TryParse(viewModel.WeightStone.ToString(), out newStone) &&
                int.TryParse(viewModel.WeightPounds.ToString(), out newPounds))
            {
                var bookingInDb = _context.Bookings.SingleOrDefault(b => b.Id == viewModel.ID);
                if (bookingInDb == null)
                {
                    return HttpNotFound();
                }

                var oldStone = bookingInDb.WeightStone;
                var oldPounds = bookingInDb.WeightPounds;

                bookingInDb.WeightStone = newStone;
                bookingInDb.WeightPounds = newPounds;
                _context.SaveChanges();

                viewModel = BookingViewModel(viewModel.ID); //recreate the viewmodel to check weight limits again

                string weightLimitMesssage;

                if (viewModel.OverWeight)
                {
                    weightLimitMesssage = "They are still over the weight limit for their airfield";
                }
                else
                {
                    weightLimitMesssage = "They are now under the weight limit for their airfield";
                }

                //send an email to the EC
                var passDictionary = new Dictionary<string, string>()
                {
                    {"oldStone", oldStone.ToString()},
                    {"oldPounds", oldPounds.ToString()},
                    {"newStone", newStone.ToString() },
                    {"newPounds", newPounds.ToString() },
                    {"weightLimitMesssage", weightLimitMesssage }

                };


                string subject = "Participant Weight Change - #" + bookingInDb.BookingNumber.ToString() + " - " +
                                 bookingInDb.FirstName + " " + bookingInDb.Surname;

                var emailsend = new Message(new SendEmail(subject, "info@skylineparachuting.co.uk", ConfigurationManager.AppSettings["EmailFolder"] + ConfigurationManager.AppSettings["DateChange"], false, passDictionary));
                emailsend.Send(null, String.IsNullOrEmpty(bookingInDb.Venue.ECEmail) ? ConfigurationManager.AppSettings["OngoingInbox"] : bookingInDb.Venue.ECEmail);

                return RedirectToAction("Index", "Booking", new { Id = viewModel.ID });
            }

            return RedirectToAction("Index", "Booking", new { Id = viewModel.ID });
        }

        //helper method to cut the clutter and return the standard VM for booking index
        // could? should? be static
        private BookingIndexViewModel BookingViewModel(int id)
        {
            var viewmodel = new BookingIndexViewModel(_context.Bookings.Include(m => m.Charity).Include(m => m.Venue).Include(m => m.VenueWeightLimit).Include(m => m.MultipleBooking).SingleOrDefault(b => b.Id == id));

            return viewmodel;
        }

      

    }
}