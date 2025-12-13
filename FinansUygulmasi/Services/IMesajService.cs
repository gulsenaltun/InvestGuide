using FinansUygulmasi.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IMesajService
{
    Task<List<MesajViewModel>> GetSonYorumlarAsync(int adet);
}