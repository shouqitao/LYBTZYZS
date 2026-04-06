using System;
using BCrypt.Net;
class P { static void Main() { Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("DevPass123", 11)); } }
