using System;
using System.IdentityModel.Tokens.Jwt;
var h = new JwtSecurityTokenHandler();
Console.WriteLine($""CanRead={h.CanReadToken(""invalid.jwt.token"")}"") ;
try { h.ReadJwtToken(""invalid.jwt.token""); Console.WriteLine(""ReadOK""); }
catch (Exception ex) { Console.WriteLine(ex.GetType().Name + "": "" + ex.Message); }
