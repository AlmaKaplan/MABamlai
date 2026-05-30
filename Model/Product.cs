namespace MABamlai.Model
{
    public class Product
    {
        private string productName;
        private string history;
        private string category;
        private int amuont;
        private int id;


        public Product(string productName, string category, int amuont, string history, int id)
        {
            this.productName = productName;
            this.category = category;
            this.amuont = amuont;
            this.history = history;
            this.id = id;

        }

        public string GetProductName() { return this.productName; }
        public string GetCategory() { return this.category; }
        public int Getamuont() { return this.amuont; }
        public string GetHistory() { return this.history; }
        public int GetId() { return this.id; }
    }
}
