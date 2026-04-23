namespace Eva.Data
{
    public static class CountryCityCatalog
    {
        private static readonly Dictionary<string, List<string>> Data = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Argentina"] = new List<string>
            {
                "Buenos Aires", "Córdoba", "Rosario", "Mendoza", "La Plata", "San Miguel de Tucumán", "Mar del Plata", "Salta"
            },
            ["Bolivia"] = new List<string>
            {
                "La Paz", "Santa Cruz de la Sierra", "Cochabamba", "Sucre", "Oruro", "Potosí", "Tarija"
            },
            ["Brasil"] = new List<string>
            {
                "São Paulo", "Río de Janeiro", "Brasilia", "Salvador", "Fortaleza", "Belo Horizonte", "Curitiba", "Porto Alegre", "Recife"
            },
            ["Chile"] = new List<string>
            {
                "Santiago", "Valparaíso", "Concepción", "La Serena", "Antofagasta", "Temuco", "Puerto Montt", "Iquique"
            },
            ["Colombia"] = new List<string>
            {
                "Bogotá", "Medellín", "Cali", "Barranquilla", "Cartagena", "Cúcuta", "Bucaramanga", "Pereira", "Santa Marta"
            },
            ["Costa Rica"] = new List<string>
            {
                "San José", "Alajuela", "Cartago", "Heredia", "Puntarenas", "Liberia", "Limón", "San Francisco de Heredia", "Desamparados", "Escazú"
            },
            ["Cuba"] = new List<string>
            {
                "La Habana", "Santiago de Cuba", "Camagüey", "Holguín", "Santa Clara", "Guantánamo", "Bayamo"
            },
            ["Ecuador"] = new List<string>
            {
                "Quito", "Guayaquil", "Cuenca", "Santo Domingo", "Machala", "Manta", "Portoviejo", "Ambato"
            },
            ["El Salvador"] = new List<string>
            {
                "San Salvador", "Santa Ana", "San Miguel", "Soyapango", "Santa Tecla", "Mejicanos", "Apopa"
            },
            ["España"] = new List<string>
            {
                "Madrid", "Barcelona", "Valencia", "Sevilla", "Zaragoza", "Málaga", "Murcia", "Palma", "Bilbao", "Alicante"
            },
            ["Estados Unidos"] = new List<string>
            {
                "Nueva York", "Los Ángeles", "Chicago", "Houston", "Miami", "Phoenix", "Dallas", "Seattle", "Denver", "Atlanta"
            },
            ["Guatemala"] = new List<string>
            {
                "Ciudad de Guatemala", "Mixco", "Villa Nueva", "Petapa", "Quetzaltenango", "Escuintla", "Cobán"
            },
            ["Honduras"] = new List<string>
            {
                "Tegucigalpa", "San Pedro Sula", "Choloma", "La Ceiba", "El Progreso", "Choluteca", "Comayagua"
            },
            ["México"] = new List<string>
            {
                "Ciudad de México", "Guadalajara", "Monterrey", "Puebla", "Tijuana", "León", "Juárez", "Torreón", "Querétaro", "Mérida", "Cancún"
            },
            ["Nicaragua"] = new List<string>
            {
                "Managua", "León", "Masaya", "Chinandega", "Matagalpa", "Estelí", "Granada"
            },
            ["Panamá"] = new List<string>
            {
                "Ciudad de Panamá", "San Miguelito", "Colón", "David", "La Chorrera", "Santiago", "Chitré"
            },
            ["Paraguay"] = new List<string>
            {
                "Asunción", "Ciudad del Este", "San Lorenzo", "Luque", "Capiatá", "Lambaré", "Fernando de la Mora"
            },
            ["Perú"] = new List<string>
            {
                "Lima", "Arequipa", "Trujillo", "Chiclayo", "Piura", "Cusco", "Iquitos", "Huancayo", "Tacna"
            },
            ["Puerto Rico"] = new List<string>
            {
                "San Juan", "Bayamón", "Carolina", "Ponce", "Caguas", "Guaynabo", "Mayagüez", "Arecibo"
            },
            ["República Dominicana"] = new List<string>
            {
                "Santo Domingo", "Santiago de los Caballeros", "La Romana", "San Pedro de Macorís", "Puerto Plata", "La Vega", "San Cristóbal"
            },
            ["Uruguay"] = new List<string>
            {
                "Montevideo", "Salto", "Paysandú", "Las Piedras", "Rivera", "Maldonado", "Mercedes"
            },
            ["Venezuela"] = new List<string>
            {
                "Caracas", "Maracaibo", "Valencia", "Barquisimeto", "Maracay", "Ciudad Guayana", "Barcelona", "Maturín"
            }
        };

        public static List<string> GetCountries()
        {
            List<string> list = new List<string>(Data.Keys);
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        public static List<string> GetCities(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
            {
                return new List<string>();
            }

            if (!Data.TryGetValue(country, out List<string>? cities) || cities == null)
            {
                return new List<string>();
            }

            List<string> copy = new List<string>(cities);
            copy.Sort(StringComparer.OrdinalIgnoreCase);
            return copy;
        }

        public static string BuildAddress(string country, string city)
        {
            if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(city))
            {
                return string.Empty;
            }

            return country.Trim() + ", " + city.Trim();
        }
    }
}
