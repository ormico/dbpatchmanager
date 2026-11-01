using NUnit.Framework;

namespace Ormico.DbPatchManager.CLI.Tests
{
    [TestFixture]
    public class ProgramExitCodeTests
    {
        [TestCase("version")]
        [TestCase("--version")]
        [TestCase("help")]
        [TestCase("--help")]
        [TestCase("build", "--help")]
        [TestCase("init", "--help")]
        public void Main_HelpOrVersionRequest_ReturnsZero(params string[] args)
        {
            Assert.That(Program.Main(args), Is.EqualTo(0));
        }

        [TestCase("notaverb")]
        [TestCase()]
        public void Main_InvalidArguments_ReturnsOne(params string[] args)
        {
            Assert.That(Program.Main(args), Is.EqualTo(1));
        }
    }
}
