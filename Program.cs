using MiniCurl.Parsing;
using System;

class Program
{
    static void Main(string[] args)
    {
        URLParser uRLParser = new URLParser();

        uRLParser.ParseURL("https://www.example.com:8080/");

    }
}