using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KanamApp.DbModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.Controllers
{
    public class DcController : Controller
    {
        protected readonly dbContext _context;
        public DcController(dbContext context)
        {
            _context = context;
        }
      
        [HttpGet]
        public JsonResult getAllDCList([FromQuery]string order, [FromQuery]string page, [FromQuery] string filter, [FromQuery]string branch, [FromQuery] DateTime fromdate, [FromQuery] DateTime todate)
        {
           order = order != null ? order : "ameToLower";
            page = page != null ? page : "1";
            filter = filter != null ? filter : "";
            //Entity 
            string orderbyCol = "ameToLower", orderByType = "", colname = "", inval = page;
            int offset = 0, rowcount = 10, num = 0;
            bool chk = Int32.TryParse(inval, out num);
            offset = num == 1 ? offset = 0 : (offset = rowcount * (num - 1));
            orderbyCol = order[0] != '-' ? order.Substring(1) : order;
            orderByType = order[0] == '-' ? "ASC" : "DESC";
            try
            {
                DateTime dt = new DateTime();
                dt = DateTime.Today;
                DateTime fdt, tdt = new DateTime();
                fdt = DateTime.Today;
                tdt = DateTime.Today;
                if (fromdate != null)
                    fdt = Convert.ToDateTime(fromdate);
                if (todate != null)
                    tdt = Convert.ToDateTime(todate);
                int branchid = 0;
                if (!string.IsNullOrEmpty(branch))
                    branchid = Convert.ToInt32(branch);
                int qcount = 0;
                DateTime secondarydt = new DateTime();
                secondarydt = (DateTime.Today).AddDays(1);
                if (filter.Contains("-"))
                {
                    filter = filter.Split('-')[0].Trim();
                }
                else
                {
                    //8/26/2018
                    string tempfilter = filter;
                    try
                    {
						DateTime tempdt = Convert.ToDateTime(filter);
                        filter = String.Format("{0:M/dd/yyyy}", tempdt);
                    }
                    catch (Exception ex)
                    {
                        filter = tempfilter;
                    }
                }
                string fromdt = fdt.Date.ToString("yyyy-MM-dd");
                string todt = tdt.Date.ToString("yyyy-MM-dd");
                string commondt = fdt.Date.ToString("yyyy-MM-dd");
                string _datequery = "convert(varchar, CreatedDate, 23) >=  '" + fromdt + "' and convert(varchar, CreatedDate, 23) <= '" + todt + "'";
                if (string.Equals(fromdt, todt))
                {
                    _datequery = "convert(varchar, CreatedDate, 23) ='" + commondt + "' ";
                }
                string qrystr = "select *  from dbo.X_DC where BranchId = " + branchid + " and Status > 0 and " + _datequery + " and xdcNumber like '%" + filter + "%' ";
                var blogs = _context.Dc.FromSql(qrystr).ToList();
                string orderby = string.Empty;
                if (orderByType == "ASC" && (orderbyCol == "-ltToLower" || orderbyCol == "otToLower"))
                    orderby = "order by ltNumber asc";
                else if (orderByType == "DESC" && (orderbyCol == "-ltToLower" || orderbyCol == "otToLower"))
                    orderby = "order by ltNumber desc";
                else if (orderByType == "ASC" && (orderbyCol == "-dcToLower" || orderbyCol == "cToLower"))
                    orderby = "order by xdcNumber asc";
                else if (orderByType == "DESC" && (orderbyCol == "-dcToLower" || orderbyCol == "cToLower"))
                orderby = "order by xdcNumber desc";
                else if (orderByType == "ASC" && (orderbyCol == "-branchToLower" || orderbyCol == "ranchToLower"))
                    orderby = "order by BranchId asc";
                else if (orderByType == "DESC" && (orderbyCol == "-branchToLower" || orderbyCol == "ranchToLower"))
                    orderby = "order by BranchId desc";
                else orderby = "order by DcNumber asc";
                var filterquery = blogs.GroupBy(g => g.DcNumber).Select(xdc => new
                {
                    DcId = xdc.Key,//new value
                    dcNumber = xdc.Key,
                    count = blogs.Where(dr => dr.DcNumber == xdc.Key).Count(),
                    date = blogs.Where(dr => dr.DcNumber == xdc.Key).FirstOrDefault().CreatedDate,
                    xdclist = blogs.Where(dr => dr.DcNumber == xdc.Key).Select(lt => new
                    {
                        dcId = lt.xdcId,
                        date = lt.CreatedDate,
                        branchId = lt.BranchId,//new value
                        productId = lt.ProductId,//new value
                        ltid = lt.Id,
                        ltnumber = (_context.lt.SingleOrDefault(x => x.Id == lt.Id).ltNumber),
                        branchname = (_context.Brch.SingleOrDefault(x => x.Id == lt.BranchId && x.Flag > 0).BranchName),
                        branchcode = (_context.Brch.SingleOrDefault(x => x.Id == lt.BranchId && x.Flag > 0).BranchCode),
                        suppliername = _context.Suppliers.SingleOrDefault(v => v.Id == lt.SupplierId).SupplierName,
                        suppliercode = lt.SupplierCode,
                        Ds = (lt.NoofDs == null || lt.NoofDs == 0)? 0: lt.NoofDs,
                        sinofrom = (lt.DsinoFrom == null) ? "0" : lt.DsinoFrom.ToString(),
                        sinoto = (lt.DsinoTo == null) ? "0" : lt.DsinoTo.ToString(),
                        quantity = (lt.Weight == null) ? "0" : ((double)(lt.Weight)).ToString("00.00"),
                        estatexdcpercentage = (lt.xdcpercentage == null) ? "0" : ((double)(lt.xdcpercentage)).ToString("00.000"),
                        xdckgs = (lt.xdckgs == null) ? "0" : ((double)(lt.xdckgs)).ToString("00.00"),
                        Vla = (lt.Vla == null) ? 0d: lt.Vla,
                        ltxdcpercentage = (lt.ltxdcpercentage == null) ? "0" : ((double)(lt.ltxdcpercentage)).ToString("00.000"),
                        ltxdckgs = (lt.ltxdckgs == null) ? "0" : ((double)(lt.ltxdckgs)).ToString("00.00"),
                        xdcnumber = lt.xdcNumber
                    })
                });
                var qdata = (filterquery).Skip(offset).Take(rowcount);
                qcount = (filterquery).ToList().Count();
                

                return Json(new { count = qcount, data = qdata,  listforxdcsearch = getAllxdcDateListforxdcList(branchid, fromdate, todate).Value, errormsg = "nil" });
            }
            catch(Exception ex)
            {
                return Json(new { count = 0, data = "null",  listforxdcsearch = "null", errormsg= ex.ToString()});
            }
        }
        [HttpGet]
        public JsonResult getAllxdcDateListforxdcList([FromQuery] int branchid, [FromQuery] DateTime fromdate, [FromQuery] DateTime todate)
        {
            try
            {
                DateTime fdt, tdt = new DateTime();
                fdt = DateTime.Today;
                tdt = DateTime.Today;
                if (fromdate != null)
                    fdt = Convert.ToDateTime(fromdate);
                if (todate != null)
                    tdt = Convert.ToDateTime(todate);
                if (branchid == 0)
                    return Json(new { data = 0d, status = false });
                string fromdt = fdt.Date.ToString("yyyy-MM-dd");
                string todt = tdt.Date.ToString("yyyy-MM-dd");
                string commondt = fdt.Date.ToString("yyyy-MM-dd");
                string _datequery = "convert(varchar, CreatedDate, 23) >=  '" + fromdt + "' and convert(varchar, CreatedDate, 23) <= '" + todt + "'";
                if (string.Equals(fromdt, todt))
                {
                    _datequery = "convert(varchar, CreatedDate, 23) ='" + commondt + "' ";
                }
                var blogs = _context.appxdc.FromSql("select *  from dbo.X_DC where BranchId = " + branchid + " and Status > 0 and " + _datequery ).ToList();
                var data = blogs.Select(s => new
                {
                    s.ltNumber,
                    s.DcNumber,
                    supplierName = _context.appsupplier.SingleOrDefault(w => w.supplierId == s.supplierId).SupplierName,
                    s.CreatedDate
                }).ToList();

                return Json(new { data = data, status = true });
            }
            catch (Exception e)
            {
                return Json(new { data = 0, status = false });
            }
        }
        [HttpGet]
        public JsonResult getProductList()
        {
            using (dbContext con = new dbContext())
            {
                if (con.appProduct.Any())
                {
                    var datalist = con.appProduct.Select(pro => new { pro.ProductId, pro.ProductName, pro.ProductType }).Where(x => x.ProductType == "Material").ToList();
                    return Json(new { data = datalist, status = true });
                }
                else
                {
                    return Json(new { data = "null", status = false });
                }
            }
        }

        [HttpPost]
        public JsonResult updatexdc([FromBody]appxdc _xdc)
        {
            using (dbContext context = new dbContext())
            {
                try
                {
                    if (_xdc != null)
                    {
                        appxdc updatedxdc = context.appxdc.Where<appxdc>(x => x.xdcId == _xdc.xdcId).SingleOrDefault<appxdc>();
                        var ltdetail = context.applt.SingleOrDefault(s => s.ltId == _xdc.ltId);
                        updatedxdc.cmonth = (_xdc.cmonth == null) ? "" : _xdc.cmonth; 
                        updatedxdc.weight = _xdc.weight == null? 0d : _xdc.weight;
                        updatedxdc.AdjxdcPercentage = _xdc.AdjxdcPercentage == null ? 0d : _xdc.AdjxdcPercentage;
                        updatedxdc.Adjxdckgs = _xdc.Adjxdckgs == null ? 0d : _xdc.Adjxdckgs;
                        updatedxdc.BottomMessage = _xdc.BottomMessage == null ? "" : _xdc.BottomMessage; 
                        updatedxdc.Remarks = _xdc.Remarks == null ? "" : _xdc.Remarks;
                        updatedxdc.CreatedDate = _xdc.CreatedDate;
                        updatedxdc.Status = 1;
                        updatedxdc.DeliveryNo = _xdc.DeliveryNo == null ?  ltdetail.SBillNo == null ? "" : ltdetail.SBillNo : _xdc.DeliveryNo;
                        updatedxdc.DeliveryDate = ltdetail.SBillDate;
                        updatedxdc.ChallanNumber = ltdetail.BranchChallanNo;
                        updatedxdc.Billno = _xdc.Billno == null ? ltdetail.Billno == null ? "" : ltdetail.Billno : _xdc.Billno;
                        updatedxdc.BillnoQuantity = _xdc.BillnoQuantity == null ? ltdetail.WtWeight == null ? 0d : ltdetail.WtWeight : _xdc.BillnoQuantity;
                        
                        updatedxdc.BillnoValue = _xdc.BillnoValue == null ? ltdetail.Amount == null ? 0d : ltdetail.Amount : _xdc.BillnoValue;
                        context.appxdc.Update(updatedxdc);
                        context.SaveChanges();
                        //update lt
                        var uplt = context.applt.SingleOrDefault(s => s.ltId == _xdc.ltId);
                        //uplt.NoofDs = (int)_xdc.NoofDs;
                        //uplt.Tanker = _xdc.Tanker;
                        uplt.DsinoFrom = _xdc.DsinoFrom;
                        uplt.DsinoTo = _xdc.DsinoTo;
                       // uplt.Weight = _xdc.Weight;
                       // uplt.xdc = _xdc.xdcpercentage;
                        //uplt.ltxdc = _xdc.ltxdcpercentage;
                       // uplt.short = _xdc.short;
                       // uplt.LDs = _xdc.LDs;
                        //uplt.LSlno = _xdc.LSlno;
                        //uplt.LKgs = _xdc.LKgs;
                        uplt.WtWeight = _xdc.BillnoQuantity;
                        uplt.Amount = _xdc.BillnoValue;
                        uplt.Billno = _xdc.Billno;
                        uplt.SBillNo = _xdc.DeliveryNo;
                        uplt.SBillDate = _xdc.DeliveryDate;
                        uplt.xdcNumber = _xdc.xdcNumber;
                        //lt Status become xdc
                        uplt.Status = 2;
                        context.applt.Update(uplt);
                        context.SaveChanges();
                        int xdc = _xdc.xdcId;
                        return Json(new { data = xdc, message = _xdc.xdcNumber + " xdc details updated successfully", status = true });
                    }
                    else
                    {
                        return Json(new { data = "0", message = "Error", status = false, errormsg="nil" });
                    }
                }
                catch (Exception e)
                {
                    return Json(new { data = "0", message = "Error", status = false, errormsg = e.ToString() });
                }
            }
        }

        public string delete(int id)
        {
            var delxdc = _context.appxdc.SingleOrDefault(dr => dr.xdcId == id);
            var xdc = _context.appxdc.Remove(delxdc);
            _context.SaveChanges();
            return delxdc.ltNumber;
        }

        [HttpGet]
        public JsonResult deletexdc([FromQuery] string xdcno)
        {
            using (dbContext con = new dbContext())
            {
                if (!string.IsNullOrEmpty(xdcno))
                {
                    var xdclist = con.appxdc.Where(xdc => xdc.xdcNumber == xdcno);
                    foreach(var xdc in xdclist)
                    {
                        con.appxdc.Remove(xdc);
                    }
                    con.SaveChanges();
                    return Json(new { data = xdcno, message = xdcno + " is deleted successfully", status = true });
                }
                else
                    return Json(new { data = "0", message = "Error", status = false });
            }
        }
        [HttpGet]
        public JsonResult xdcDeleteById([FromQuery] int id)
        {
            using (dbContext con = new dbContext())
            {
                if (id!=0)
                {
                    var xdclist = con.appxdc.SingleOrDefault(xdc => xdc.xdcId == id);
                    
                        con.appxdc.Remove(xdclist);
                    
                    con.SaveChanges();
                    return Json(new { data = id, message = id + " is deleted successfully", status = true });
                }
                else
                    return Json(new { data = "0", message = "Error", status = false });
            }
        }
        [HttpGet]
        public JsonResult getAllltNumber([FromQuery]int branchid, [FromQuery]int productid, [FromQuery]int supplierid, [FromQuery]string code, [FromQuery] string xdcnumber, [FromQuery] string date)
        {
            if (_context.applt.Any())
            {
                
                    var data = from x in _context.applt
                               where x.BranchId == branchid && x.ProductId == productid && x.SupplierId == supplierid && x.SupplierCode == code
                               select new
                               {
                                   x.ltId,
                                   x.ltNumber,
                                   Ds = x.NoofDs,
                                   date = ((DateTime)x.CreatedDate).ToString("dd-MM-yyyy")
                               };


                    return Json(new { data = data, status = true });
               
            }
            else
            {
                return Json(new { data = "null", status = false });
            }
        }
        [HttpGet]
        public JsonResult getltNumberDetail([FromQuery]int ltid,  [FromQuery]int supplierid, [FromQuery] string xdcnumber)
         {

            if (_context.appxdc.Any(x => x.ltId == ltid))
            {
                var _xdcnumber = _context.appxdc.Where(w => w.ltId == ltid).FirstOrDefault().xdcNumber;
                return Json(new
                {
                    data = "null",
                    message = "error",
                    status = false,
                    checkltdata = "1",
                    xdcnumber = _xdcnumber,
                    checkltmessage = "This lt Number " + _context.applt.SingleOrDefault(s => s.ltId == ltid).ltNumber + " Already Existed in this xdc " + _xdcnumber,
                    checkltstatus = false
                });
            }

            else
            {

                bool available = _context.applt.Any(x => (!string.IsNullOrEmpty(x.DsinoFrom) || x.DsinoFrom != "0") && x.ltId == ltid);
                if (available)
                {
                    using (dbContext con = new dbContext())
                    {
                        try
                        {
                            double lessxdcvalue = 0d;
                            if (supplierid != 0)
                            {
                                var sd = con.appsupplier.SingleOrDefault(s => s.supplierId == supplierid).Lessxdc;
                                    lessxdcvalue = (sd  == null)? 0d : (double)sd ;
                            }
                            var blogs = con.applt.FromSql("select *  from dbo.app_lt where ltId = " + ltid).ToList();
                            int pid = blogs.FirstOrDefault().ProductId;
                            double _Vla = 0d; _Vla = (double)blogs.FirstOrDefault().Vla;
                            var productDetails = con.appProduct.SingleOrDefault(kp => kp.ProductId == pid);
                            var _lNarr = productDetails.lNarration;
                            var _lgrademessage = (_Vla > productDetails.VlaCtrlValue) ? productDetails.lGradeMessage : "";
                            var _shortNarr = productDetails.shortNarration;
                            var _shortvalue = blogs.FirstOrDefault().short;
                            var _Weight = blogs.FirstOrDefault().Weight;
                            var _LKgs = blogs.FirstOrDefault().LKgs;
                            double LKgsvalue = (_LKgs == null) ? 0d : (double)_LKgs;
                            var _xdc = blogs.FirstOrDefault().xdc;
                            double Weight = 0d;
                            bool shortiscalculated = false;
                            if(_shortvalue != 0 && _Weight != 0 )
                            {
                                Weight = (double)_Weight - (double)_shortvalue;
                                shortiscalculated = true;
                            }
                            else
                            {
                                Weight = (double)_Weight;
                            }
                            
                            if(_LKgs != null && _LKgs != 0 && _Weight != 0 )
                            {
                                if (shortiscalculated)
                                {
                                    Weight = (double)Weight - (double)_LKgs;
                                }
                                else
                                {
                                    Weight = (double)_Weight - (double)_LKgs;
                                }
                            }
                            else
                            {
                                Weight = (double)_Weight;
                            }
                            
                            var _noofDs = blogs.FirstOrDefault().NoofDs;
                            var _LDs = blogs.FirstOrDefault().LDs;
                            int totDs = (int)((_LDs != null || _LDs != 0) ? (_noofDs - _LDs) : _noofDs);

                            double _lessxdccalculated = 0d;
                            if(_xdc != null )
                                    _lessxdccalculated = (double)_xdc - lessxdcvalue;
                           
                            var datalist =
                                blogs.Select(s => new
                                {
                                    ltNumber = s.ltNumber.ToString(),
                                    noofDs = (s.NoofDs != null) ? totDs.ToString() : "0",
                                    Vla = (s.Vla != 0 && s.Vla != null) ? s.Vla.ToString() : "0",
                                    tanker = (s.Tanker != null) ? s.Tanker : 0,
                                    DsinoFrom = (s.DsinoFrom != null) ? s.DsinoFrom : "0",
                                    DsinoTo = (s.DsinoTo != null) ? s.DsinoTo : "0",
                                    Weight = Weight,
                                    xdc = (s.xdc == null || s.xdc == 0) ? "0.000" : _lessxdccalculated.ToString(),
                                    ltxdc = (s.ltxdc == null || s.ltxdc == 0) ? "0.000" : s.ltxdc.ToString(),
                                    xdckgs = ((s.xdc != 0 || s.xdc != null)&&(lessxdcvalue!=0 )) ? (Math.Round((double)(Weight * (_lessxdccalculated / 100)))) : 0,
                                    loxdckgs = s.ltxdc != null || s.ltxdc != 0 ? Math.Round((double)(Weight * (s.ltxdc / 100))) : 0,

                                    short =(s.short != null) ? s.short.ToString() : "0",
                                    LKgs = (s.LKgs != null) ? s.LKgs.ToString() : "0",
                                    
                                    lslno = (s.LSlno != null) ? s.LSlno.ToString() : "0",
                                    LDs = (s.LDs != null) ? s.LDs.ToString() : "0",
                                    lxdckgs = (LKgsvalue !=0 && _lessxdccalculated!=0)?((float)Math.Round(LKgsvalue * (_lessxdccalculated / 100))):0d,
                                    DeliveryNo = (s.SBillNo != null) ? s.SBillNo.ToString() : "",
                                    Billnono = (s.Billno != null) ? s.Billno.ToString() : "",
                                    BillnoQuantity = (s.WtWeight != null) ? s.WtWeight.ToString() : "0",
                                    BillnoValue = (s.Amount != null) ? s.Amount.ToString() : "0",
                                    shortMessage = (s.short != 0 && s.short != null) ? s.short.ToString() + ' ' +
                                    _shortNarr.ToString() : "",
                                    lGrade = _lgrademessage.ToString(),
                                    lMessage = (!string.IsNullOrEmpty(s.LSlno) && s.LSlno != "0") ? _lNarr.ToString() + ' ' + s.LSlno : ""
                                }).FirstOrDefault();
                            if (lessxdcvalue == 0)
                            {
                                return Json(new { data = datalist, message = "Success", status = true, checkltdata = "0", xdcnumber = xdcnumber, checkltmessage = "lt Available, Warning! Less xdc Settings not found", checkltstatus = true, });
                            }
                            else
                            {
                                return Json(new { data = datalist, message = "Success", status = true, checkltdata = "0", xdcnumber = xdcnumber, checkltmessage = "lt Available", checkltstatus = true, });
                            }
                        }
                        catch(Exception ex)
                        {
                            return Json(new { data = "null", message = "Data Loading Failed", status = false, checkltdata = "0", xdcnumber = xdcnumber, checkltmessage = "lt Available & Data Loading Failed", checkltstatus = true, ErrorMsg = ex.ToString() });
                        }
                    }  
                }
                else
                        {
                            return Json(new { data = "null", message = "Success", status = false, checkltdata = "0", xdcnumber = xdcnumber, checkltmessage = "Drum Si No and Net Weight is not updated", checkltstatus = false});
                        }

            }

            //if (_context.appxdc.Any(x => x.ltId == ltid))
            //    return Json(new { data = "null", message = "xdc is created for this lt, Go to Edit mode", status = false});
                
            }
        
        [HttpGet]
        public JsonResult getxdcNumberDetail([FromQuery]int ltid, [FromQuery]int xdcid)
        {
            try
            {
                if (_context.appxdc.Any())
                {
                   

                    var datalist = _context.appxdc.FromSql("select *  from dbo.app_xdc where xdcId = " + xdcid).FirstOrDefault();
                   

                    int pid = datalist.ProductId;
                    double _Vla = 0d; _Vla = (double)datalist.Vla;
                    var productDetails = _context.appProduct.SingleOrDefault(kp => kp.ProductId == pid);
                    var _lNarr = productDetails.lNarration;
                    var _lgrademessage = (_Vla > productDetails.VlaCtrlValue) ? productDetails.lGradeMessage : "";
                    var _shortNarr = productDetails.shortNarration;
                 
                   
                    return Json(new { data = datalist, message = "Success", status = true });
                }
                else
                {
                    return Json(new { data = "null", message = "lt Number Detail not Found", status = false });
                }
            }catch(Exception ex) {
                return Json(new { data = "null", message = "lt Number Detail not Found", status = false, ErrorMSg = ex.ToString() });

            }
        }

        [HttpGet]
        public JsonResult checkltavailablity([FromQuery] int ltid, [FromQuery] string xdcnumber)
        {
            try
            {
                if (_context.appxdc.Any(x => x.ltId == ltid))
                {
                    var _xdcnumber = _context.appxdc.Where(w => w.ltId == ltid).FirstOrDefault().xdcNumber;
                    return Json(new
                    {
                        data = "1",
                        xdcnumber = _xdcnumber,
                        message = "This lt Number " + _context.applt.SingleOrDefault(s => s.ltId == ltid).ltNumber + " Already Existed in this xdc " + _xdcnumber,
                        status = false
                    });

                }

                else
                {

                    bool available = _context.applt.Any(x => (!string.IsNullOrEmpty(x.DsinoFrom) || x.DsinoFrom != "0") && x.ltId == ltid);
                    if (available)
                        return Json(new { data = "0", xdcnumber = xdcnumber, message = "lt Available", status = true });
                    else
                        return Json(new { data = "0", xdcnumber = xdcnumber, message = "Drum Si No and Net Weight is not updated", status = false });

                }
            }
            catch (Exception ex) {
                return Json(new { data = "0", xdcnumber = xdcnumber, message = "lt Available", status = true });
            }
        }


        [HttpPost]
        public JsonResult addxdc([FromBody]appxdc _xdc)

        {
            try
            {
                if (_xdc.BranchId == 0)
                    return Json(new { data = "0", message = "Branch name is not valid", status = false });
                if (_xdc.ProductId ==0)
                    return Json(new { data = "0", message = "Item name is not valid", status = false });
                if (string.IsNullOrEmpty(_xdc.VoucherType))
                    return Json(new { data = "0", message = "Voucher name is not valid", status = false });
                if (_xdc.supplierId == 0)
                    return Json(new { data = "0", message = "supplier name is not valid", status = false });
                if (string.IsNullOrEmpty(_xdc.xdcNumber))
                    return Json(new { data = "0", message = "supplier name is not valid", status = false });

                bool flag = false;
                using (var xdccontext = new dbContext())
                {
                    flag = xdccontext.appxdc.Any(x => x.BranchId == _xdc.BranchId && x.xdcNumber == _xdc.xdcNumber && x.ltId == _xdc.ltId);
                }

                    if (_xdc != null)
                {
                    //Validation
                    
                    _xdc.Vla = Double.IsNaN((double)_xdc.Vla) ? 0d : _xdc.Vla;
                    _xdc.xdcpercentage = Double.IsNaN((double)_xdc.xdcpercentage) ? 0d : _xdc.xdcpercentage;
                    _xdc.xdckgs = Double.IsNaN((double)_xdc.xdckgs) ? 0d : _xdc.xdckgs;
                    _xdc.ltxdcpercentage = Double.IsNaN((double)_xdc.ltxdcpercentage) ? 0d : _xdc.ltxdcpercentage;
                    _xdc.ltxdckgs = Double.IsNaN((double)_xdc.ltxdckgs) ? 0d : _xdc.ltxdckgs;
                    _xdc.short = Double.IsNaN((double)_xdc.short) ? 0d : _xdc.short;
                    _xdc.LKgs = Double.IsNaN((double)_xdc.LKgs) ? 0d : _xdc.LKgs;
                    _xdc.lxdckgs = Double.IsNaN((double)_xdc.lxdckgs) ? 0d : _xdc.lxdckgs;
                    _xdc.AdjxdcPercentage = Double.IsNaN((double)_xdc.AdjxdcPercentage) ? 0d : _xdc.AdjxdcPercentage;
                    _xdc.Adjxdckgs = Double.IsNaN((double)_xdc.Adjxdckgs) ? 0d : _xdc.Adjxdckgs;
                    _xdc.weight = Double.IsNaN((double)_xdc.weight) ? 0d : _xdc.weight;
                    _xdc.LSlno = (_xdc.LSlno == null) ? "0" : _xdc.LSlno;
                    if (!flag)
                    {
                        using (var xdccontext = new dbContext())
                        {
                            var ltdetail = xdccontext.applt.SingleOrDefault(s => s.ltId == _xdc.ltId);
                            _xdc.Status = 1;
                            
                            _xdc.DeliveryNo = string.IsNullOrEmpty(_xdc.DeliveryNo) ? ltdetail.SBillNo : _xdc.DeliveryNo;
                            if (ltdetail.SBillDate != null)
                                _xdc.DeliveryDate = ltdetail.SBillDate;
                            _xdc.ChallanNumber = string.IsNullOrEmpty(ltdetail.BranchChallanNo) ? "" : ltdetail.BranchChallanNo;
                            _xdc.Billno = (string.IsNullOrEmpty(_xdc.Billno)) ? (string.IsNullOrEmpty(ltdetail.Billno) ? "" : ltdetail.Billno) : _xdc.Billno;
                            _xdc.BillnoQuantity = (_xdc.BillnoQuantity == 0d || _xdc.BillnoQuantity ==null)?((ltdetail.WtWeight == 0d) ? 0d: ltdetail.WtWeight): _xdc.BillnoQuantity;
                            _xdc.BillnoValue = (_xdc.BillnoValue == 0d || _xdc.BillnoValue == null) ? ((ltdetail.Amount == 0d) ? 0d : ltdetail.Amount) : _xdc.BillnoValue;
                            _xdc.ltVlano = Double.IsNaN((double)_xdc.ltVlano) ? 0d : _xdc.ltVlano;
                            _xdc.ltNumber = ltdetail.ltNumber;
                            //dbcontext.Entry(_xdc).State = EntityState.Added;
                            _xdc.xdcId = 0;
                            xdccontext.appxdc.Add(_xdc);
                            xdccontext.SaveChanges();
                        }

                        //update lt
                        using (var ltcontext = new dbContext())
                        {
                            var uplt = ltcontext.applt.SingleOrDefault(s => s.ltId == _xdc.ltId);
                           
                           
                       //     uplt.Tanker = _xdc.Tanker;
                            uplt.DsinoFrom = _xdc.DsinoFrom;
                            uplt.DsinoTo = _xdc.DsinoTo;
                            uplt.Vla = _xdc.Vla;
                            
                          
                            
                            uplt.SBillNo = string.IsNullOrEmpty(_xdc.DeliveryNo) ? uplt.SBillNo : _xdc.DeliveryNo;
                            
                            uplt.Billno = (string.IsNullOrEmpty(_xdc.Billno)) ? "":_xdc.Billno;
                            uplt.WtWeight = (_xdc.BillnoQuantity == 0d || _xdc.BillnoQuantity == null) ? 0d : _xdc.BillnoQuantity;
                            uplt.Amount = (_xdc.BillnoValue == 0d || _xdc.BillnoValue == null) ? 0d : _xdc.BillnoValue;
                            uplt.xdcNumber = _xdc.xdcNumber;
                          
                            uplt.Status = 2;
                            ltcontext.applt.Update(uplt);
                            ltcontext.SaveChanges();
                        }

                        int xdc = _xdc.xdcId;
                        return Json(new { data = xdc, message = _xdc.xdcNumber + " xdc Number is Successfully Created", status = true });

                    }
                    else
                    {

                        using (var xdccontext = new dbContext())
                        {
                            appxdc updatedxdc = xdccontext.appxdc.Where<appxdc>(x => x.xdcId == _xdc.xdcId).SingleOrDefault<appxdc>();
                            var ltdetail = _context.applt.SingleOrDefault(s => s.ltId == _xdc.ltId);
                            updatedxdc.TopSupplierName = _xdc.TopSupplierName;
                            updatedxdc.NoofDs = _xdc.NoofDs;
                            updatedxdc.DsinoFrom = _xdc.DsinoFrom;
                            updatedxdc.DsinoTo = _xdc.DsinoTo;
                            updatedxdc.Vla = _xdc.Vla;
                            updatedxdc.Weight = _xdc.Weight;
                            updatedxdc.xdcpercentage = _xdc.xdcpercentage;
                            updatedxdc.xdckgs = _xdc.xdckgs;
                            updatedxdc.ltxdcpercentage = _xdc.ltxdcpercentage;
                            updatedxdc.ltxdckgs = _xdc.ltxdckgs;
                            updatedxdc.short = _xdc.short;
                            updatedxdc.CropMonth = _xdc.CropMonth;
                            updatedxdc.LDs = _xdc.LDs;
                            updatedxdc.LSlno = _xdc.LSlno;

                            updatedxdc.LKgs = _xdc.LKgs;
                            updatedxdc.AdjCropMonth = _xdc.AdjCropMonth;
                            updatedxdc.weight = _xdc.weight;
                            updatedxdc.AdjxdcPercentage = _xdc.AdjxdcPercentage;
                            updatedxdc.Adjxdckgs = _xdc.Adjxdckgs;
                            updatedxdc.BottomMessage = _xdc.BottomMessage;
                            updatedxdc.Remarks = _xdc.Remarks;

                            updatedxdc.CreatedDate = _xdc.CreatedDate;

                            updatedxdc.Status = 1;
                            updatedxdc.DeliveryNo = ltdetail.SBillNo;
                            updatedxdc.DeliveryDate = ltdetail.SBillDate;
                            updatedxdc.ChallanNumber = ltdetail.BranchChallanNo;
                            //updatedxdc.Billno = ltdetail.Billno;
                            updatedxdc.Billno = (string.IsNullOrEmpty(_xdc.Billno)) ? (string.IsNullOrEmpty(ltdetail.Billno) ? "" : ltdetail.Billno) : _xdc.Billno;
                            updatedxdc.BillnoQuantity = (_xdc.BillnoQuantity == 0d || _xdc.BillnoQuantity == null) ? ((ltdetail.WtWeight == 0d) ? 0d : ltdetail.WtWeight) : _xdc.BillnoQuantity;
                            updatedxdc.BillnoValue = (_xdc.BillnoValue == 0d || _xdc.BillnoValue == null) ? ((ltdetail.Amount == 0d) ? 0d : ltdetail.Amount) : _xdc.BillnoValue;


                            xdccontext.appxdc.Update(updatedxdc);
                           xdccontext.SaveChanges();
                        }
                        //update lt
                        using (var ltcontext = new dbContext())
                        {
                            var uplt = ltcontext.applt.SingleOrDefault(s => s.ltId == _xdc.ltId);
                           // uplt.NoofDs = (int)_xdc.NoofDs;
                          //  uplt.Tanker = _xdc.Tanker;
                            uplt.Vla = _xdc.Vla;
                            uplt.DsinoFrom = _xdc.DsinoFrom;
                            uplt.DsinoTo = _xdc.DsinoTo;
                          //  uplt.Weight = _xdc.Weight;
                          //  uplt.xdc = _xdc.xdcpercentage;
                         //   uplt.Approxxdc = _xdc.ltxdcpercentage;
                          //  uplt.ltxdcKgs = _xdc.ltxdckgs;
                          //  uplt.short = _xdc.short;
                         //   uplt.LDs = _xdc.LDs;
                          //  uplt.LSlno = _xdc.LSlno;
                          //  uplt.LKgs = _xdc.LKgs;
                            //lt Status become xdc
                           
                            uplt.SBillNo = string.IsNullOrEmpty(_xdc.DeliveryNo) ? uplt.SBillNo : _xdc.DeliveryNo;
                            uplt.Billno = (string.IsNullOrEmpty(_xdc.Billno)) ? "" : _xdc.Billno;
                            uplt.WtWeight = (_xdc.BillnoQuantity == 0d || _xdc.BillnoQuantity == null) ? 0d : _xdc.BillnoQuantity;
                            uplt.Amount = (_xdc.BillnoValue == 0d || _xdc.BillnoValue == null) ? 0d : _xdc.BillnoValue;

                            uplt.xdcNumber = _xdc.xdcNumber;
                        //    uplt.ClosedDate = _xdc.CreatedDate;
                            uplt.Status = 2;


                            ltcontext.applt.Update(uplt);
                            ltcontext.SaveChanges();
                        }
                        int xdc = _xdc.xdcId;
                        return Json(new { data = xdc, message = _xdc.xdcNumber + " xdc details updated successfully", status = true, errormsg = "0" });
                    }


                }
                else
                {
                    return Json(new { data = "0", message = "Error", status = false, errormsg = "0" });
                }
            }
            catch(Exception ex)
            {
                return Json(new { data = "0", message = "Error", status = false, errormsg = ex.ToString()});
            }
            
        }
        [HttpGet]
        public JsonResult checkxdcNumberAvailablility([FromQuery]string xdcnumber, [FromQuery]int id, [FromQuery]int BranchId, [FromQuery] string vouchertype)
        {

            bool isAvailable=false;
            var getltnumber = _context.appxdc
                .Where(w => w.BranchId == BranchId && w.VoucherType == vouchertype)
                              .OrderByDescending(x => x.xdcId)
                              .Select(x => x.xdcNumber)
                              .FirstOrDefault();

            if (id == 0)
            {
                // xdcnumber && x.BranchId == BranchId);
                isAvailable = true;
                if (getltnumber != null)
                    xdcnumber = getltnumber.ToString();
            }
            else
            {
                isAvailable = _context.appxdc.Any(x => x.xdcNumber == xdcnumber && !(x.xdcId == id) && x.BranchId == BranchId && x.VoucherType == vouchertype);
            }
            //get settings
            var settings = _context.appArrivalSettings.SingleOrDefault(x => x.BranchId == BranchId && x.Type == vouchertype);
            int codelength = settings.VoucherBeginNo.Length;
            int letterlength = xdcnumber.Length - codelength;
            int xdcvdigits = (int)settings.VoucherDigits;
            string ltprefix = xdcnumber.Split('/')[0];
            string ltsuffix = xdcnumber.Split('/')[1];
            string tempsuffixnumber = string.Empty;
            string templtnumber = string.Empty;
            string message = string.Empty;
            if(ltsuffix.Any(char.IsLetter))
                return Json(new { availability = isAvailable, newsuffixnumber = "", suggestltnumber = "", message = message, valid= false });
            if (isAvailable)
            {
                int _suffixval = Convert.ToInt32(ltsuffix);
                _suffixval++;
                int suffixlength = _suffixval.ToString().Length;
                string zerosStr = "";
                for(int i=0; i<codelength; i++)
                {
                    zerosStr = zerosStr + "0";
                }
                zerosStr = zerosStr.Remove(zerosStr.Length - suffixlength);
                string suffix = zerosStr+ _suffixval.ToString();
                tempsuffixnumber = suffix;
                templtnumber = ltprefix + suffix;
                message = "Suggest " + templtnumber + " is Available";
            }
            else
            {
                if (ltsuffix != settings.VoucherBeginNo)
                {
                    tempsuffixnumber = settings.VoucherBeginNo;
                    templtnumber = settings.VoucherNoPrefix.Trim() + settings.VoucherBeginNo.Trim();
                }
                else
                {
                    tempsuffixnumber = ltsuffix.Trim();
                    templtnumber = ltprefix.Trim() + ltsuffix.Trim();
                }
                message = "lt number " + templtnumber + " is Available";
            }
            if (getltnumber == null){
                tempsuffixnumber = ltsuffix.Trim();
                templtnumber = ltprefix.Trim() + ltsuffix.Trim();
                message = "lt number " + templtnumber + " is Available";
            }
            return Json(new { availability = isAvailable, newsuffixnumber = tempsuffixnumber, suggestltnumber = templtnumber, message = message, valid = true });
        }
        [HttpGet]
        public JsonResult getVlaControlValue([FromQuery]int branchid)
        {
            if (_context.appxdc.Any(x => x.BranchId == getCurrentBanchId()))
                return Json(new { data = "null", message = "xdc not found", status = false });
            if (_context.appxdc.Any(x => x.BranchId == getCurrentBanchId()))
            {
                var datalist = _context.appxdc.Select(v => new { v.BranchId, v.xdcNumber }).Where(x => x.BranchId == getCurrentBanchId()).ToList();
                return Json(new { data = datalist, message = "Success", status = true });
            }
            else
            {
                return Json(new { data = "null", message = "xdc not found", status = false });
            }
        }


       

        //============================Calculation=========================================================
        [HttpGet]
        public JsonResult checkVlaValue([FromQuery]int productid, [FromQuery]float Vla)
        {

            if (_context.appProduct.Any(x => x.ProductId == productid))
            {
                if(float.IsNaN(Vla))
                {
                    return Json(new { data = "null", message = "Please Enter the Vla Value", status = false });

                }
                var datalist = _context.appProduct.Select(v => new { v.ProductId, v.ProductName, v.VlaCtrlValue,
                    lGradeMessage = (Vla > v.VlaCtrlValue)? v.lGradeMessage: "", 
                    v.shortNarration, v.lNarration, v.DrumWeight, v.MaxDmWeight }).SingleOrDefault(x => x.ProductId == productid);



                return Json(new { data = datalist, message = "Success", status = true });
            }
            else
            {
                return Json(new { data = "null", message = "Item not found to check Vla Value", status = false });
            }
        }
        [HttpGet]
        public JsonResult calculateWeight([FromQuery] int productid, [FromQuery] int Ds)
        {
            try
            {
                if (productid == 0 || Ds==0)
                    return Json(new { data = 0d,message = "Error! No Required Values!", status = false });
                //get drum quantity  
                double MaxDmWeight = (double)_context.appProduct.SingleOrDefault(kp =>kp.ProductId == productid).MaxDmWeight;
                if (Ds == 0d)
                    return Json(new { data = 0d, message = "No Ds", status = false });
                
                Double Weight = Math.Round((MaxDmWeight * Ds));
                return Json(new { data = Weight.ToString("00.00"), message = "", status = true });
            }
            catch (Exception e)
            {
                return Json(new { data = 0d,message ="Error", status = false });
            }
        }
        [HttpGet]
        public JsonResult checkWeightByMaxDmWeight([FromQuery] int productid, [FromQuery] int Ds, [FromQuery] double Weight)
        {
            try
            {
                if (productid == 0 || Ds == 0 || Weight == 0 || double.IsNaN(Weight))
                    return Json(new { totalWeight = 0d, Weight=0d, status = false });
                //get drum quantity  
                double MaxDmWeight = (double)_context.appProduct.SingleOrDefault(kp => kp.ProductId == productid).MaxDmWeight;
                if (Ds == 0d)
                    return Json(new { totalWeight = 0d, Weight = 0d, status = false });

                Double totalWeight = Math.Round((MaxDmWeight * Ds));
                if(Weight > totalWeight)
                {
                    return Json(new { totalWeight = totalWeight.ToString("00.00"), Weight = Weight.ToString("00.00"), Weightallow = false, status = true });
                }
                else
                {
                    return Json(new { totalWeight = Weight.ToString("00.00"), Weight = Weight.ToString("00.00"), Weightallow = true, status = false });
                }
                
            }
            catch (Exception e)
            {
                return Json(new { totalWeight = 0d, Weight = Weight, status = false });
            }
        }

        [HttpGet]
        public JsonResult calculatexdcPercentage([FromQuery] float xdckgs, [FromQuery] float Weight, [FromQuery] int supplierId)
        {
            try
            {
                if ( xdckgs == 0 || Weight == 0 || supplierId == 0 || double.IsNaN(Weight))
                    return Json(new { xdcpercentage = 0d, lessxdc = 0d, status = false });
                double xdcp = MathCalculate(xdckgs, Weight).result;
                double lessxdc = (double)_context.appsupplier.SingleOrDefault(kl => kl.supplierId == supplierId).Lessxdc;
                xdcp = xdcp - lessxdc;
                string _xdcp = xdcp.ToString("00.000");
                _xdcp = _xdcp.Replace('-', '0');
                return Json(new { xdcpercentage = _xdcp, lessxdc= lessxdc.ToString("00.000"), Weight = 0d, status = true });
            }
            catch (Exception e)
            {
                return Json(new { xdcpercentage = 0d, lessxdc = 0d, status = false });
            }
        }

        [HttpGet]
        public JsonResult calculateLessxdcPercentage([FromQuery] float xdckgs, [FromQuery] float Weight, [FromQuery] int supplierId)
        {
            try
            {
                if (xdckgs == 0 || Weight == 0 || supplierId == 0 || double.IsNaN(Weight) || double.IsNaN(xdckgs))
                    return Json(new { xdcpercentage = 0d, lessxdc = 0d, status = false });
                double xdcp = MathCalculate(xdckgs, Weight).result;
                double lessxdc = (double)_context.appsupplier.SingleOrDefault(kl => kl.supplierId == supplierId).Lessxdc;
                xdcp = xdcp - lessxdc;
                string _xdcp = xdcp.ToString("00.000");
                _xdcp = _xdcp.Replace('-', '0');
                return Json(new { xdcpercentage = _xdcp, lessxdc = lessxdc.ToString("00.000"), Weight = 0d, status = true });
            }
            catch (Exception e)
            {
                return Json(new { xdcpercentage = 0d, lessxdc = 0d, status = false });
            }
        }
        [HttpGet]
        public JsonResult calltxdcPercentage([FromQuery] float ltxdckgs, [FromQuery] float Weight, [FromQuery] int supplierId)
        {
            try
            {
                if (ltxdckgs == 0 || Weight == 0 || supplierId == 0 || double.IsNaN(Weight) || double.IsNaN(ltxdckgs))
                    return Json(new { xdcpercentage = 0d,  status = false });
                double xdcp = MathCalculate(ltxdckgs, Weight).result;
                //double lessxdc = (double)_context.appsupplier.SingleOrDefault(kl => kl.supplierId == supplierId).Lessxdc;
                //xdcp = xdcp - lessxdc;
                string _xdcp = xdcp.ToString("00.000");
                return Json(new { xdcpercentage = _xdcp, Weight = 0d, status = true });
            }
            catch (Exception e)
            {
                return Json(new { xdcpercentage = 0d,  status = false });
            }
        }
        [HttpGet]
        public JsonResult calculateDWeight([FromQuery] float xdcper, [FromQuery] float Weight)
        {
            try
                {
                if (xdcper == 0 || Weight == 0 || double.IsNaN(xdcper) || double.IsNaN(Weight))
                    return Json(new { data = 0d, status = false });

                double xdckgs = Math.Round(Weight * (xdcper / 100));
                string _xdckgs = xdckgs.ToString("00.00");
               
                return Json(new { data = _xdckgs, Weight = 0d, status = true });
            }
            catch (Exception e)
            {
                return Json(new { data = 0d, status = false });
            }
        }
        [HttpGet]
        public JsonResult calculateltDWeight([FromQuery] float xdcper, [FromQuery] float Weight)
        {
            try
            {
                if (xdcper == 0 || Weight == 0 || double.IsNaN(xdcper) || double.IsNaN(Weight))
                    return Json(new { data = 0d, status = false });

                double xdckgs = Math.Round(Weight * (xdcper / 100));
                string _xdckgs = xdckgs.ToString("00.00");
               
                return Json(new { data = _xdckgs, Weight = 0d, status = true });
            }
            catch (Exception e)
            {
                return Json(new { data = 0d, status = false });
            }
        }
        [HttpGet]
        public JsonResult calculateWeightByshort([FromQuery] float short, [FromQuery] float Weight, [FromQuery] int productid)
        {
            try
            {
                if (Weight == 0 || productid == 0 || double.IsNaN(short) || double.IsNaN(Weight))
                    return Json(new { Weight = 0d, shortmessage="", status = false });

                double upWeight = Math.Round(Weight - short);
                string shortmessage = short.ToString() + ' ' + _context.appProduct.SingleOrDefault(kp => kp.ProductId == productid).shortNarration.ToString();
                return Json(new { Weight = upWeight.ToString("00.00"), shortmessage= shortmessage, status = true });
            }
            catch (Exception e)
            {
                return Json(new { data = 0d, status = false });
            }
        }
        [HttpGet]
        public JsonResult calculateLDsByDs([FromQuery] int Ds, [FromQuery] int LDs, [FromQuery] int productid, [FromQuery] string LSlno)
                    {
            try
            {
                if (Ds == 0 ||  productid == 0 )
                    return Json(new { totDs = Ds, shortmessage = "", Dscalculated = 1, status = false });

                if(LDs > Ds)
                    return Json(new { totDs = Ds, shortmessage = "", Dscalculated = 0, status = false });
                int totDs = (Ds - LDs);
                string shortmessage = "";
                if (!string.IsNullOrEmpty(LSlno) && LSlno != "0")
                {
                    shortmessage = _context.appProduct.SingleOrDefault(kp => kp.ProductId == productid).lNarration.ToString() + ' ' + LSlno;
                }
                
                return Json(new { totDs = totDs, shortmessage = shortmessage, Dscalculated = 0, status = true });
            }
            catch (Exception e)
            {
                return Json(new { totDs = 0d, shortmessage = "", Dscalculated = 1, status = false });
            }
        }
        
        public JsonResult calculatelDWeight([FromQuery] float LKgs, [FromQuery] float xdcper,[FromQuery] float tempxdcWeightFromshort)
                {
            try
        {
                if (xdcper == 0 || tempxdcWeightFromshort == 0 || double.IsNaN(LKgs) || double.IsNaN(xdcper))
                {
                    var Weights = tempxdcWeightFromshort - LKgs;
                    return Json(new { lxdcKgs = 0d, Weight = Weights.ToString("00.00"), status = false });
                }
                float lxdcKgs = (float)Math.Round(LKgs * (xdcper / 100));
               var Weight = tempxdcWeightFromshort - LKgs;
                return Json(new { lxdcKgs = lxdcKgs.ToString("00.00"), Weight = Weight.ToString("00.00"), status = true });
            }
            catch (Exception e)
            {
                return Json(new { lxdcKgs = 0d, Weight = 0d, status = false });
            }
        }
        [HttpGet]
        public JsonResult calculateAdjustDWeight([FromQuery] float Weight, [FromQuery] float xdcper)
        {
            try
            {
                if (Weight == 0 || xdcper == 0 || double.IsNaN(Weight) || double.IsNaN(xdcper) )
                    return Json(new { adjxdcKgs =0d, Weight = 0d, status = false });

                float AdjxdcKgs = (float)Math.Round(Weight * (xdcper / 100));
                return Json(new { adjxdcKgs = AdjxdcKgs.ToString("00.00"), status = true });
            }
            catch (Exception e)
            {
                return Json(new { adjxdcKgs =0d, Weight = 0d, status = false });
            }
        }
        [HttpGet]
        public percentageMath MathCalculate([FromQuery]float count, [FromQuery]float target)
        {
            percentageMath p = new percentageMath();
            p.target = target;
            float x = 0f, percentage = 0f;
            p.achieve = count;
            float diff = (100 - p.target);
            x = 100 * (diff / p.target);
            percentage = ((p.achieve / diff) * x);
            p.result = percentage;
            return p;
        }
    }
    public struct percentageMath
    {
        public float target { get; set; }
        public float achieve { get; set; }
        public float result { get; set; }

    }
}