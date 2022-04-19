using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalWebApi.Models
{
    public class Fornecedor
    {
        public int Id { get; set; }

        public string? Nome { get; set; }

        public string? Documento { get; set; }

        public bool Ativo { get; set; }
    }
}
