using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTCPathPlanning
{
    public class CubicFunction
    {
        //ax^3 + bx^2 + cx + d
        double a, b, c, d;
        public CubicFunction(double a, double b, double c, double d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public int Degree
        {
            get
            {
                if(a == 0)
                {
                    if(b == 0)
                    {
                        if(c == 0)
                        {
                            return 0;
                        }
                        return 1;
                    }
                    return 2;
                }
                return 3;
            }
        }

        public double Evaluate(double x)
        {
            return Math.Pow(x, 3) * a + Math.Pow(x, 2) * b + x * c + d;
        }

        public CubicFunction Differentiate()
        {
            return new CubicFunction(0, 3 * a, 2 * b, c);
        }

        public double IntegrateLength(double startX, double endX)
        {
            CubicFunction deriv = Differentiate();            
            switch(Degree)
            {
                case 0:
                    return endX - startX;
                case 1:
                    CubicFunction integral = new CubicFunction(0, 0, Math.Sqrt(Math.Pow(deriv.d, 2) + 1), 0);
                    return integral.Evaluate(endX) - integral.Evaluate(startX);
                case 2:
                    double integralEndValue = (Math.Sqrt(Math.Pow((deriv.c * endX + deriv.d), 2) + 1) * (deriv.c * endX + deriv.d)
                        + arsinh(deriv.c * endX + deriv.d)) / (2 * deriv.c);
                    double integralStartValue = (Math.Sqrt(Math.Pow((deriv.c * startX + deriv.d), 2) + 1) * (deriv.c * startX + deriv.d)
                        + arsinh(deriv.c * startX + deriv.d)) / (2 * deriv.c);
                    return integralEndValue - integralStartValue;
                case 3:
                    return 0;
            }
            throw new Exception("Something went very wrong here...");
        }

        private double arsinh(double x)
        {
            //Inverse Hyperbolic Sine HArcsin(X) = Log(X + Sqr(X * X + 1))
            return Math.Log(x + Math.Sqrt(x * x + 1));
        }
    }
}
