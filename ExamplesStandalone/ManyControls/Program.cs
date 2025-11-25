namespace ManyControls;

public class Program
{
    public static void Main(string[] args)
    {
        var useXaml = true;
        IProgram program = useXaml ? new ProgramFromXaml() : new ProgramfromCode();
        program.Main(args);
    }
}