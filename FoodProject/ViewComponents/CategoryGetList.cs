﻿using ProductProject.Data.Models;
using ProductProject.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ProductProject.ViewComponents
{
	public class CategoryGetList:ViewComponent
	{
		public IViewComponentResult Invoke()
		{
			Context context= new Context();
            var categoryList = context.Categories.Where(x => x.Status == true).ToList();
            return View(categoryList);
		}
	}
}
