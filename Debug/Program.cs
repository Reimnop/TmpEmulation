using TmpIO;

using var stream = File.Open("test.tmpe", FileMode.Open);
var file = TmpRead.Read(stream);
Console.WriteLine(file.Metadata.Name);