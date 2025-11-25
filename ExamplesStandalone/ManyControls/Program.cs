namespace ManyControls;

public class Program
{
    public static void Main(string[] args)
    {
        var useXaml = true;
        IProgram program = useXaml ? new ProgramFromXaml() : new ProgramFromCode();
        program.Main(args);
    }
}