﻿using ProductProject.Data;
using ProductProject.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Schema;

namespace ProductProject.Controllers
{
    [AllowAnonymous]
    [Authorize]
    public class OrderController : Controller
    {
        Context context = new Context();
        public IActionResult Index(int id)
        {
            ShoppingViewModel p = new ShoppingViewModel();
            if (User.Identity.IsAuthenticated) // sisteme otantike olmuşsa sepeti görüntüleyecek
            {
                var userName = User.Identity.Name;
                var userId = context.Users.Where(x => x.UserName == userName).Select(y => y.Id).FirstOrDefault();

                var Product = context.Products.Find(id);
                p.AppUserID = userId;
                p.ProductID = Product.ProductID;

                p.ProductPrice = Product.Price;

                Shopping shopping = new Shopping()
                {
                    ProductID = p.ProductID,
                    AppUserID = p.AppUserID,
                    ShoppingPrice = p.ProductPrice,
                };

                context.Shoppings.Add(shopping);
                context.SaveChanges();
                return RedirectToAction("Index", "Default");
            }
            else
            {
                return RedirectToAction("Index", "Product");
            }

        }
        public IActionResult BasketDetails()
        {
            Payment payment = new Payment();

            var userName = User.Identity.Name;
            var userID = context.Users.Where(x => x.UserName == userName).Select(y => y.Id).FirstOrDefault();
            var shopping = context.Shoppings.Where(x => x.AppUser.UserName == userName).Include(y => y.Product).ToList();
            var ordersCount = context.OrderDetails.Where(x => x.AppUserID == userID).Count();
            ViewBag.OrdersCount = ordersCount; // Verdiği sipariş sayısı


            if (User.Identity.IsAuthenticated) // sisteme otantike olmuşsa sepeti görüntüleyecek
            {
                var basket = context.Shoppings.Where(x => x.AppUserID == userID).ToList();

                ViewBag.TotalPrice = basket.Sum(x => x.ShoppingPrice * x.ShoppingQuantity);

                if (basket.Count() != 0)
                {
                    ViewBag.basketCount = basket.Count();
                }
                else
                {
                    ViewBag.basketCount = "Ürün bulunmamaktadır.";
                }
                return View(basket);
            }
            else
            {
                return RedirectToAction("Index", "Category");
            }

        }
        public IActionResult DeleteProduct(int id)
        {
            var deleteID = context.Shoppings.Find(id);
            context.Shoppings.Remove(deleteID);
            context.SaveChanges();
            return RedirectToAction("BasketDetails", "Order");
        }
        public IActionResult PlusProduct(int id)
        {
            var plusID = context.Shoppings.Find(id);
            plusID.ShoppingQuantity += 1;
            context.SaveChanges();
            return RedirectToAction("BasketDetails", "Order");
        }

        public IActionResult MinusProduct(int id)
        {
            var minusID = context.Shoppings.Find(id);
            minusID.ShoppingQuantity -= 1;
            context.SaveChanges();
            return RedirectToAction("BasketDetails", "Order");
        }
        [HttpGet]
        public IActionResult PaymentAdd()
        {

            return View();
        }
        [HttpPost]
        public IActionResult PaymentAdd(Payment payment)
        {
            if (!ModelState.IsValid)
            {
                var userName = User.Identity.Name;
                var userID = context.Users.Where(x => x.UserName == userName).Select(y => y.Id).FirstOrDefault();
                var paymentID = context.Shoppings.Where(x => x.AppUser.Id == userID).Include(y => y.AppUser).Select(y => y.AppUserID).FirstOrDefault();

                var basket = context.Shoppings.Where(x => x.AppUserID == userID).ToList(); // Giriş yapan kullanıcıya ait sepetteki ürünleri listeler
                foreach (var item in basket)
                {

                    if (item.AppUserID != payment.AppUserID)
                    {
                        payment.AppUserID = paymentID;    // Giriş yapan kullanıcının id'si ile ödeme yapma işlemi
                        payment.ShoppingTotal = basket.Sum(x => x.ShoppingPrice * x.ShoppingQuantity); // ödenilen toplam fiyatı yansıttık

                        context.Payments.Add(payment);
                        context.SaveChanges();

                        while (basket.Count() != 0)
                        {
                            // Ürün stock güncelleme işlemi
                            var shoppingProductId = context.Shoppings.Where(x => x.AppUserID == userID).Select(y => y.ProductID).FirstOrDefault();
                            var Products = context.Products.Find(shoppingProductId);
                            if (shoppingProductId == 0) // Sepette ürün kalmayınca döngünün kırılması için
                            {
                                break;
                            }
                            Products.Stock -= item.ShoppingQuantity;

                            var removeId = context.Shoppings.Where(x => x.AppUserID == userID).Select(y => y.ShoppingID).FirstOrDefault(); // giriş yapan kullanıcının id'si Shopping tablosunda varsa o kaydı seç
                            if (removeId == 0)   // Sepette ürün kalmayınca döngünün kırılması için
                            {
                                break;
                            }

                            // Ürünleri sepetten kaldırmadan önce OrderDetail adında tabloya ekleyoruz(Bu tablo admin panelinde sipariş detayları için gerekli)

                            OrderDetail orderDetail = new OrderDetail();
                            orderDetail.ProductName = Products.Name;
                            orderDetail.ProductPrice = Products.Price;
                            orderDetail.ProductImage = Products.ImageURL;
                            orderDetail.ProductStock = Products.Stock;
                            orderDetail.AppUserID = item.AppUserID;
                            orderDetail.ProductQuantity = item.ShoppingQuantity;
                            orderDetail.ProductOrderDate = item.ShoppingDate;
                            context.OrderDetails.Add(orderDetail);
                            context.SaveChanges();

                            // Sipariş verdikten sonra ürünleri sepetten kaldırma işlemi

                            var id = context.Shoppings.Find(removeId); // seçilen kaydı Shopping tablosunda bul
                            context.Shoppings.Remove(id); // ve bulunan kaydı sil
                            context.SaveChanges();
                        }
                    }
                }

                return RedirectToAction("Index", "Default");
            }

            return View();
        }
        public IActionResult UserOrders()
        {
            var userName = User.Identity.Name;
            var userID = context.Users.Where(x => x.UserName == userName).Select(y => y.Id).FirstOrDefault();
            var ordersCount = context.OrderDetails.Where(x => x.AppUserID == userID).Count();
            ViewBag.OrdersCount = ordersCount; // Verdiği sipariş sayısı
            if (ordersCount != 0) // Sipariş sayısı 0'a eşit değilse bu siparişleri listele
            {
                var userOrders = context.OrderDetails.Where(x => x.AppUserID == userID).ToList();
                return View(userOrders);
            }
            else
            {
                ViewBag.ordersMessage = "Aktif siparişiniz bulunmamaktadır.";
            }
            return View();
        }
    }
}
