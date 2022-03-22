using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using OnlineStoreDataAccess.models;
using System.Collections.Generic;
using PagedList;
using System.Net.Http.Headers;
using OnlineStore.APIAuth;
using OnlineStoreDataAccess.edmx;
using System.Text;
using System.IO;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace OnlineStore.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        #region declarations
        private readonly string apiUrl = ConfigurationManager.AppSettings["WebAPIurl"].ToString();
        private readonly Utils utls = new Utils();
        private JwtAuthToken jwtAuthToken;
        #endregion

        private string GetToken()
        {
            //get jwt token
            if (Session["Token"] != null)
            {
                jwtAuthToken = (JwtAuthToken)Session["Token"];

                //if token expired, retrive new token
                if(jwtAuthToken.ExpirationTime < DateTime.Now)
                {
                    JwtAuth jwtAuth = new JwtAuth();
                    jwtAuthToken = jwtAuth.JwtAuthToken().Result;
                    Session["Token"] = jwtAuthToken;
                }
            }
            else
            {
                JwtAuth jwtAuth = new JwtAuth();
                jwtAuthToken = jwtAuth.JwtAuthToken().Result;
                Session["Token"] = jwtAuthToken;
            }

            return jwtAuthToken.Token;
        }

        public async Task<ActionResult> Index(string search = "", int? page = 0, string sort = "")
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string api = ConfigurationManager.AppSettings["WebAPIurl"].ToString();

                    //get all products from api
                    client.BaseAddress = new Uri(@"" + api + @"api/products");
                    var httpContent = new HttpRequestMessage(HttpMethod.Get, @"?search=" + search);

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetToken());

                    var response = client.SendAsync(httpContent).Result;
                    var contents = await response.Content.ReadAsStringAsync();
                    contents = contents.TrimStart('\"');
                    contents = contents.TrimEnd('\"');
                    contents = contents.Replace("\\", "");
                    var products = JsonConvert.DeserializeObject<List<productmodel>>(contents);
                    var productList = new OnlineStoreBusinessLogic.products.product
                    {
                        ListOfProducts = products
                    };

                    ViewBag.SortNameParameter = string.IsNullOrEmpty(sort) ? "ProductName desc" : "";

                    List<productmodel> lstProducts = new List<productmodel>();
                    //sort products
                    switch (ViewBag.SortNameParameter)
                    {
                        case "":
                            lstProducts = productList.ListOfProducts.OrderBy(x => x.productname).ToList();
                            break;
                        case "ProductName desc":
                            lstProducts = productList.ListOfProducts.OrderByDescending(x => x.productname).ToList();
                            break;
                    }

                    if (page > 0)
                        return View(lstProducts.ToPagedList(page ?? 1, 4));
                    else
                        return View(lstProducts.ToPagedList(1, 4));
                }
            }
            catch(Exception ex)
            {
                utls.Log("Index" + ex.Message);
                return View();
            }
        }

        private async Task<productmodel> GetProductById(Guid itemid)
        {
            //Get Product By Id
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(@"" + apiUrl + @"api/productbyid");
                var httpContent = new HttpRequestMessage(HttpMethod.Get, @"?itemid=" + itemid);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetToken());
                var response = client.SendAsync(httpContent).Result;
                var contents = await response.Content.ReadAsStringAsync();
                contents = contents.TrimStart('\"');
                contents = contents.TrimEnd('\"');
                contents = contents.Replace("\\", "");
                var product = JsonConvert.DeserializeObject<productmodel>(contents);
                                
                return product;
            }
        }

        public ActionResult GetVehicleImage(Guid itemid)
        {
            try
            {
                byte[] mybytearray = GetProductById(itemid).Result.productimage;

                return File(mybytearray, "image/jpg");
            }
            catch(Exception ex)
            {
                utls.Log("GetVehicleImage" + ex.Message);
                return File(new byte[0], "image/jpg");
            }
        }

        public ActionResult AddToCart(Guid productId, string url)
        {
            try
            {
                //if cart is empty, add new order item to cart session
                if (Session["cart"] == null)
                {
                    List<productmodel> cart = new List<productmodel>();
                    var product = GetProductById(productId).Result;
                    cart.Add(new productmodel()
                    {
                        itemid = product.itemid,
                        productname = product.productname,
                        quantity = 1,
                        price = product.price
                    });
                    Session["cart"] = cart;
                }
                else
                {
                    List<productmodel> cart = (List<productmodel>)Session["cart"];
                    var count = cart.Count();
                    var product = GetProductById(productId).Result;
                    for (int i = 0; i < count; i++)
                    {
                        //if same item added to cart, increase quantity
                        if (cart[i].itemid == productId)
                        {
                            int prevQty = cart[i].quantity;
                            cart.Remove(cart[i]);
                            cart.Add(new productmodel()
                            {
                                itemid = product.itemid,
                                productname = product.productname,
                                quantity = prevQty + 1,
                                price = product.price
                            });
                            break;
                        }
                        else
                        {
                            //add new cart item
                            var prd = cart.Where(x => x.itemid == productId).SingleOrDefault();
                            if (prd == null)
                            {
                                cart.Add(new productmodel()
                                {
                                    itemid = product.itemid,
                                    productname = product.productname,
                                    quantity = 1,
                                    price = product.price
                                });
                            }
                        }
                    }
                    //store cart item(s) in a session
                    Session["cart"] = cart;
                }
            }
            catch(Exception ex)
            {
                utls.Log("AddToCart" + ex.Message);
            }
            return Redirect(url);
        }

        public ActionResult ViewOrder()
        {
            return View();
        }

        public ActionResult RemoveFromCart(Guid productId)
        {
            try
            {
                //remove cart item(s)
                List<productmodel> cart = (List<productmodel>)Session["cart"];
                foreach (var item in cart)
                {
                    if (item.itemid == productId)
                    {
                        cart.Remove(item);
                        break;
                    }
                }
                Session["cart"] = cart;
            }
            catch(Exception ex)
            {
                utls.Log("RemoveFromCart" + ex.Message);
            }
            return Redirect("ViewOrder");
        }

        public async Task<ActionResult> CheckoutOrder()
        {
            try
            {
                //check if cart is not empty
                if (Session["cart"] != null && ((List<productmodel>)Session["cart"]).Count() > 0)
                {
                    List<productmodel> cartItems = (List<productmodel>)Session["cart"];
                    List<orderitem> orderItems = new List<orderitem> { };
                    foreach (var item in cartItems)
                    {
                        orderItems.Add(new orderitem
                        {
                            userid = new Utils().GetToken(),
                            productid = item.itemid,
                            quantity = item.quantity,
                            unitprice = item.price,
                            lineitemtotal = item.price * item.quantity
                        });
                    }

                    using (var client = new HttpClient())
                    {
                        //save order
                        client.BaseAddress = new Uri(@"" + apiUrl);
                        string apiMethod = "api/SaveOrder";
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetToken());
                        var response = client.PostAsync(apiMethod, new StringContent(JsonConvert.SerializeObject(orderItems), Encoding.UTF8, "application/json")).Result;
                        var contents = await response.Content.ReadAsStringAsync();
                        contents = contents.TrimStart('\"');
                        contents = contents.TrimEnd('\"');
                        contents = contents.Replace("\\", "");

                        if (response.IsSuccessStatusCode && contents.Contains("BET"))
                        {
                            contents = contents.Replace("\\", "");
                            //send email if order saved successfully!!!
                            byte[] bytes = GenerateOrderPdf(contents, orderItems).ToArray();

                            utls.ProcessEmail(new Email
                            {
                                Attachment = new MemoryStream(bytes),
                                Sender = "godfreys@liquidcapital.co.za",
                                Recipient = "shongwegodfrey@gmail.com",
                                Subject = "BET Demo ORDER",
                                EmailBody = "Hi " + utls.GetUserName() + ", Please find attached purchace order.",
                                FileName = contents.ToString()
                            });

                            //clear cart session variable
                            Session["cart"] = null;
                            
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            return View();
                        }
                    }
                }
                else
                { 
                    return View(); 
                }
            }
            catch(Exception ex)
            {
                utls.Log("CheckoutOrder" + ex.Message);
                return View();
            }
        }

        public string getHtml(List<orderitem> orderitems)
        {
            try
            {
                string messageBody = "<font>The following are the records: </font><br><br>";
                if (orderitems.Count() == 0) return messageBody;
                string htmlTableStart = "<table style=\"border-collapse:collapse; text-align:center;\" >";
                string htmlTableEnd = "</table>";
                string htmlHeaderRowStart = "<tr style=\"background-color:#6FA1D2; color:#ffffff;\">";
                string htmlHeaderRowEnd = "</tr>";
                string htmlTrStart = "<tr style=\"color:#555555;\">";
                string htmlTrEnd = "</tr>";
                string htmlTdStart = "<td style=\" border-color:#5c87b2; border-style:solid; border-width:thin; padding: 5px;\">";
                string htmlTdEnd = "</td>";
                messageBody += htmlTableStart;
                messageBody += htmlHeaderRowStart;
                messageBody += htmlTdStart + "Product Name: " + htmlTdEnd;
                messageBody += htmlTdStart + "Quantity: " + htmlTdEnd;
                messageBody += htmlTdStart + "Unit Price: " + htmlTdEnd;
                messageBody += htmlTdStart + "Sub Total:" + htmlTdEnd;
                messageBody += htmlHeaderRowEnd;
                //Loop all the order items from grid vew and added to html td  
                foreach (var item in orderitems)
                {
                    messageBody = messageBody + htmlTrStart;
                    messageBody = messageBody + htmlTdStart + GetProductById(item.productid).Result.productname + htmlTdEnd; //Print Product
                    messageBody = messageBody + htmlTdStart + item.quantity + htmlTdEnd; //Print Qty
                    messageBody = messageBody + htmlTdStart + item.unitprice + htmlTdEnd; //Print Unit Price
                    messageBody = messageBody + htmlTdStart + item.lineitemtotal + htmlTdEnd; //Print Sub Total
                    messageBody = messageBody + htmlTrEnd;
                }
                messageBody = messageBody + htmlTableEnd;
                return messageBody; // return HTML Table as string from this function  
            }
            catch (Exception ex)
            {
                utls.Log("getHtml" + ex.Message);
                return null;
            }
        }

        public MemoryStream GenerateOrderPdf(string OrderNumber, List<orderitem> orderitems)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrderNumber))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        Document document = new Document(PageSize.A4, 10, 10, 10, 10);

                        PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                        document.Open();

                        PdfPTable table_Order = new PdfPTable(3);
                        table_Order.AddCell(utls.newCellSpace());
                        table_Order.AddCell(utls.newCellHeader("BET Demo Order Details "));
                        table_Order.AddCell(utls.newCellCol1("Order Number:"));
                        table_Order.AddCell(utls.newCellCol2(OrderNumber));
                        table_Order.AddCell(utls.newCellCol1("Order Date:"));
                        table_Order.AddCell(utls.newCellCol2(string.Format("{0:yyyy-MM-dd HH:mm tt}", DateTime.Now)));

                        document.Add(table_Order);

                        decimal totalPrice = 0;
                        foreach (var orderItem in orderitems)
                        {
                            PdfPTable table_Deatails = new PdfPTable(3);
                            table_Deatails.AddCell(utls.newCellSpace());
                            table_Deatails.AddCell(utls.newCellHeader("Order Item "));
                            //Product
                            table_Deatails.AddCell(utls.newCellCol1("Product:"));
                            table_Deatails.AddCell(utls.newCellCol2(GetProductById(orderItem.productid).Result.productname));

                            //Quantity
                            table_Deatails.AddCell(utls.newCellCol1("Quantity:"));
                            table_Deatails.AddCell(utls.newCellCol2(orderItem.quantity.ToString()));

                            //Unit Price
                            table_Deatails.AddCell(utls.newCellCol1("Unit Price:"));
                            table_Deatails.AddCell(utls.newCellCol2(orderItem.unitprice.ToString()));

                            //Unit Price
                            table_Deatails.AddCell(utls.newCellCol1("Unit Price:"));
                            table_Deatails.AddCell(utls.newCellCol2(orderItem.lineitemtotal.ToString()));
                            
                            totalPrice += (decimal)orderItem.lineitemtotal;

                            table_Deatails.AddCell(utls.newCellSpace());
                            document.Add(table_Deatails);
                        }

                        PdfPTable table_Total = new PdfPTable(3);
                        table_Total.AddCell(utls.newCellCol1("TOTAL:"));
                        table_Total.AddCell(utls.newCellCol2(totalPrice.ToString()));
                        document.Add(table_Total);

                        document.Close();

                        return memoryStream;
                    }
                }
                return new MemoryStream();
            }
            catch(Exception ex)
            {
                utls.Log("GenerateOrderPdf" + ex.Message);
                return new MemoryStream();
            }
            finally { }
        }
    }
}