namespace MABamlai.Model
{
    public class MissingEquipment
    {
        private string productName;
        private string category;
        private int amuont;


        public MissingEquipment(string productName, string category, int amuont)
        {
            this.productName = productName;
            this.category = category;
            this.amuont = amuont;

        }

        public string GetProductName() { return this.productName; }
        public string GetCategory() { return this.category; }

        public int GetAmuont() { return this.amuont; }
    }
}
