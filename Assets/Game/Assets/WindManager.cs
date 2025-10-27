namespace Game.Assets
{
    public class WindManager
    {
        // --- Warunki wiatru rzeczywistego ---
        private double V_true; // prędkość rzeczywistego wiatru [m/s]
        private double BetaTrueDeg; // kąt rzeczywistego wiatru względem dziobu [°]
        
        
        public double getWindSpeed()
        {
            return V_true;
        }
        
        public double getWindDirection()
        {
            return BetaTrueDeg;
        }
    }
}