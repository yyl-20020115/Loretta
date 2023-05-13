namespace ProtoExtracter;

public class Proto3Writer
{
    public class RefValue
    {
        public int Value;
        public RefValue(int value) => Value = value;
    }
    public readonly TextWriter Writer;
    
    protected int dent = 0;
    protected readonly Stack<RefValue> indices = new();
    protected virtual void Enter()
    {
        this.dent++;
    }
    protected virtual void Leave()
    {
        if (this.dent > 0)
        {
            this.dent--;
        }
    }
    public Proto3Writer(TextWriter writer)
    {
        this.Writer = writer;
        this.indices.Push(new(1));
    }

    public virtual void Begin()
    {
        this.WriteLine("syntax=\"proto3\";");
        this.WriteLine();
    }
    public virtual void End()
    {
    }
    public virtual void WriteMessageBegin(string name)
    {
        this.WriteLine($"message {name} {{");
        this.Enter();
        this.indices.Push(new(1));
    }
    public virtual void WriteMessageEnd()
    {
        if(this.indices.Count > 0)
        {
            this.indices.Pop();
        }
        this.Leave();
        this.WriteLine("}");
    }
    public virtual void WriteStarComment(string comment)
    {
        this.Writer.Write($"/*{comment}*/");
    }
    public virtual void WriteLineComment(string comment)
    {
        this.WriteLine($"//{comment}");
    }
    public virtual void WriteField(string name,string type,bool optional = false,bool repeated = false)
    {
        int index = this.indices.Peek().Value++;

        this.WriteLine($"{(optional?"optional ":"")}{(repeated ? "repeated " : "")}{type} {name} = {index};");
    }
    public virtual void WriteLine(string text="")
    {
        this.Writer.Write(new string(' ', this.dent * 4));
        this.Writer.WriteLine(text);
    }
}
