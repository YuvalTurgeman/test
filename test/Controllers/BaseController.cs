using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;


public class BaseController : Controller
{
    protected string UserPermission => HttpContext.Session.GetString("UserPermission") ?? "Guest";
    protected int? UserId => HttpContext.Session.GetInt32("UserId");
}
