using Microsoft.AspNetCore.Mvc;
using QRCoder;
using PFMA.Data;
using PFMA.Models;

namespace PFMA.Controllers
{
    public class BillQRCodeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BillQRCodeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BillQRCode
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var items = _context.BillQRCodes
                .Where(q => q.UserId == userId.Value)
                .OrderByDescending(q => q.QRCodeId)
                .ToList();

            return View(items);
        }

        // POST: BillQRCode/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(BillQRCode model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ModelState.Remove(nameof(BillQRCode.User));

            if (ModelState.IsValid)
            {
                model.UserId = userId.Value;
                _context.BillQRCodes.Add(model);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: BillQRCode/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var item = _context.BillQRCodes.FirstOrDefault(q => q.QRCodeId == id && q.UserId == userId.Value);
            if (item != null)
            {
                _context.BillQRCodes.Remove(item);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // GET: BillQRCode/GenerateQRCode/5  -> returns a PNG image
        [HttpGet]
        public IActionResult GenerateQRCode(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized();
            }

            var item = _context.BillQRCodes.FirstOrDefault(q => q.QRCodeId == id && q.UserId == userId.Value);
            if (item == null)
            {
                return NotFound();
            }

            string payload = $"Bill: {item.Title}\nConsumer No: {item.ConsumerNumber}\nAmount: {item.Amount:N2}";

            using (var qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] png = qrCode.GetGraphic(10);
                return File(png, "image/png");
            }
        }
    }
}
