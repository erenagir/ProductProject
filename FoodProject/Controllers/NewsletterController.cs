﻿using ProductProject.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Linq;

namespace ProductProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NewsletterController : Controller
    {
        Context context = new Context(); 

        public IActionResult Index()
        {
            var subscribeMails=context.Subscribes.ToList();
            return View(subscribeMails);
        }
        public IActionResult NewsletterDelete(int id)
        {
            var subscribeID = context.Subscribes.Find(id);
            context.Subscribes.Remove(subscribeID);
            context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
