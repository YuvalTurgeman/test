using Microsoft.AspNetCore.Mvc;

public class BaseController : Controller
{
    public BaseController()
    {
        // Populate ViewData with UserPermission from the session
        var userPermission = HttpContext.Session.GetString("UserPermission") ?? "Guest";
        ViewData["UserPermission"] = userPermission;
    }
}