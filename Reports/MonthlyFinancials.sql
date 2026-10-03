/* Monthly Financials package for the API (Reports > Monthly Financials). Parameters @Yr, @Prd, @AsOf are passed by ReportsController.
   Budget = system budget (bdglin + approved change-order cost) for every job; @JobBudget is intentionally empty.
   Source of truth for the logic: db/reports/monthly-financials.sql in the Angular repo. */
/* =============================================================================
   Monthly Financials package  -  database: StowellCompany (Sage 300 CRE schema)
   Reproduces the tabs of the "<year> - <Month> Financials ALL" workbook.
   Each result set has a "report" column that says which tab it belongs to:
     1-BalanceSheet, 1-BalanceSheet totals ............ <Month> - Balance Sheet
     2-IncomeStatement, 2b-IncomeStatement summary .... <Month> - Inc. Stmt- ALL (2b also feeds Prior Year Comparison)
     3-OverUnder (+ totals vs GL, + review) ........... <Month> - Over Under Report
     4-ARAging, 4-ARAging totals ...................... <Month> - AR Aging
     5a-FS Balance Sheet, 5b-FS Income & RE ........... FS Balance Sheet, FS Stmt of Inc & RE
     6-Check ......................................... validation only (no tab)

   To run another month change the DECLARE values below (currently AUGUST 2026).
   Also update @JobBudget in section 3 (the Over/Under budget column is hand-maintained).
   Read-only: SELECTs only (temp tables only), nothing in the database is modified.

   GL conventions (dbo.lgract / dbo.lgrbal):
     - lgrbal.balnce for acttyp 1-10 (balance sheet) is the ENDING balance of the period.
     - lgrbal.balnce for acttyp 11-17 (P&L) is the ACTIVITY of the period; YTD = SUM(actprd <= @Prd).
     - Balances are stored positive in their natural direction (dbtcrd 1 = debit, 2 = credit).
       acttyp: 1 cash, 2 receivables, 3 inventory/WIP, 4 prepaid/other assets, 5 fixed assets,
               6 accumulated depreciation, 7 current liabilities, 8 long-term liabilities,
               9 capital stock, 10 owner distributions, 11 operating income, 12 other income,
               13/14 direct expense, 15 overhead, 16 administrative, 17 income tax.
     - Retained earnings (33000) is only rolled forward for distributions, not net income:
       RE = balance of 33000 + net income of every closed year since @ReFirstYr.
   ============================================================================= */



DECLARE @ReFirstYr smallint = 2019;          -- first year whose net income is NOT already inside 33000
DECLARE @PyYr      smallint = @Yr - 1;

/* ---------------------------------------------------------------------------
   1. BALANCE SHEET
   --------------------------------------------------------------------------- */
;WITH ni AS (   -- net income by year
    SELECT b.postyr,
           SUM(CASE WHEN a.acttyp IN (11,12) THEN b.balnce ELSE -b.balnce END) AS net_income
    FROM dbo.lgrbal b
    JOIN dbo.lgract a ON a.recnum = b.lgract
    WHERE a.acttyp BETWEEN 11 AND 17
      AND (b.postyr < @Yr OR (b.postyr = @Yr AND b.actprd <= @Prd))
    GROUP BY b.postyr
), bs AS (
    SELECT CASE a.acttyp WHEN 1 THEN 10 WHEN 2 THEN 10 WHEN 3 THEN 10
                         WHEN 5 THEN 20 WHEN 6 THEN 30 WHEN 4 THEN 40
                         WHEN 7 THEN 50 WHEN 8 THEN 60
                         WHEN 9 THEN 70 WHEN 10 THEN 80 END AS sec_ord,
           CASE a.acttyp WHEN 1 THEN 'Current Assets' WHEN 2 THEN 'Current Assets' WHEN 3 THEN 'Current Assets'
                         WHEN 5 THEN 'Long Term Assets' WHEN 6 THEN 'Accumulated Depreciation'
                         WHEN 4 THEN 'Other Assets'
                         WHEN 7 THEN 'Current Liabilities' WHEN 8 THEN 'Long Term Liabilities'
                         WHEN 9 THEN 'Equity / Capital' WHEN 10 THEN 'Owner''s Drawing / Dividend' END AS section,
           a.acttyp, a.recnum AS account, a.lngnme AS account_name,
           CASE WHEN a.acttyp IN (1,2,3,4,5,10) THEN b.balnce ELSE b.balnce END AS balance,   -- natural sign
           CASE WHEN a.acttyp IN (6,10) THEN -1 ELSE 1 END AS presentation_sign
    FROM dbo.lgract a
    JOIN dbo.lgrbal b ON b.lgract = a.recnum AND b.postyr = @Yr AND b.actprd = @Prd
    WHERE a.acttyp BETWEEN 1 AND 10 AND a.recnum <> 33000 AND b.balnce <> 0
)
SELECT '1-BalanceSheet' AS report, sec_ord, section, account, account_name,
       balance * presentation_sign AS amount
FROM bs
UNION ALL
SELECT '1-BalanceSheet', 75, 'Equity / Capital', 33000, 'Retained Earnings',
       (SELECT balnce FROM dbo.lgrbal WHERE lgract = 33000 AND postyr = @Yr AND actprd = @Prd)
     + (SELECT ISNULL(SUM(net_income),0) FROM ni WHERE postyr BETWEEN @ReFirstYr AND @Yr - 1)
UNION ALL
SELECT '1-BalanceSheet', 90, 'Current Profit (Loss)', NULL, 'Current Profit (Loss) YTD',
       (SELECT ISNULL(SUM(net_income),0) FROM ni WHERE postyr = @Yr)
ORDER BY sec_ord, account;

/* Balance sheet subtotals + balance check (Assets - Liabilities - Equity must be 0.00) */
;WITH ni AS (
    SELECT b.postyr,
           SUM(CASE WHEN a.acttyp IN (11,12) THEN b.balnce ELSE -b.balnce END) AS net_income
    FROM dbo.lgrbal b JOIN dbo.lgract a ON a.recnum = b.lgract
    WHERE a.acttyp BETWEEN 11 AND 17
      AND (b.postyr < @Yr OR (b.postyr = @Yr AND b.actprd <= @Prd))
    GROUP BY b.postyr
), t AS (
    SELECT SUM(CASE WHEN a.acttyp IN (1,2,3,4,5) THEN b.balnce WHEN a.acttyp = 6 THEN -b.balnce ELSE 0 END) AS total_assets,
           SUM(CASE WHEN a.acttyp IN (7,8) THEN b.balnce ELSE 0 END) AS total_liabilities,
           SUM(CASE WHEN a.acttyp = 9 THEN b.balnce WHEN a.acttyp = 10 THEN -b.balnce ELSE 0 END) AS capital_less_dist
    FROM dbo.lgract a JOIN dbo.lgrbal b ON b.lgract = a.recnum AND b.postyr = @Yr AND b.actprd = @Prd
    WHERE a.acttyp BETWEEN 1 AND 10 AND a.recnum <> 33000
)
SELECT '1-BalanceSheet totals' AS report,
       t.total_assets, t.total_liabilities,
       t.capital_less_dist
         + (SELECT balnce FROM dbo.lgrbal WHERE lgract = 33000 AND postyr = @Yr AND actprd = @Prd)
         + (SELECT ISNULL(SUM(net_income),0) FROM ni WHERE postyr BETWEEN @ReFirstYr AND @Yr - 1)
         + (SELECT ISNULL(SUM(net_income),0) FROM ni WHERE postyr = @Yr) AS total_equity,
       t.total_assets - t.total_liabilities
         - ( t.capital_less_dist
           + (SELECT balnce FROM dbo.lgrbal WHERE lgract = 33000 AND postyr = @Yr AND actprd = @Prd)
           + (SELECT ISNULL(SUM(net_income),0) FROM ni WHERE postyr BETWEEN @ReFirstYr AND @Yr - 1)
           + (SELECT ISNULL(SUM(net_income),0) FROM ni WHERE postyr = @Yr) ) AS out_of_balance
FROM t;

/* ---------------------------------------------------------------------------
   2. INCOME STATEMENT - DETAIL BY ACCOUNT
      period | YTD | prior-year period | prior-year YTD | budget (period / YTD)
      Insurance accounts 61000 + 61010 are shown together as "General Insurance / Life"
      (as in the workbook).  State income tax (78500) is shown under Other Income as a negative.
   --------------------------------------------------------------------------- */
;WITH act AS (
    SELECT a.recnum, a.lngnme, a.acttyp,
           CASE WHEN a.recnum IN (61000,61010) THEN 61000 ELSE a.recnum END AS grp_acct,
           CASE WHEN a.recnum IN (61000,61010) THEN 'General Insurance / Life' ELSE a.lngnme END AS grp_name,
           CASE a.acttyp WHEN 11 THEN 10 WHEN 13 THEN 20 WHEN 14 THEN 20 WHEN 15 THEN 30 WHEN 16 THEN 40 WHEN 12 THEN 50 WHEN 17 THEN 50 END AS sec_ord,
           CASE a.acttyp WHEN 11 THEN 'Operating Income' WHEN 13 THEN 'Direct Expense' WHEN 14 THEN 'Direct Expense' WHEN 15 THEN 'Overhead Expense'
                         WHEN 16 THEN 'Administrative Expense' WHEN 12 THEN 'Other Income' WHEN 17 THEN 'Other Income' END AS section,
           CASE WHEN a.acttyp = 17 THEN -1 ELSE 1 END AS sgn
    FROM dbo.lgract a
    WHERE a.acttyp BETWEEN 11 AND 17
)
SELECT '2-IncomeStatement' AS report, x.sec_ord, x.section, x.grp_acct AS account, x.grp_name AS account_name,
       SUM(CASE WHEN b.postyr = @Yr   AND b.actprd  = @Prd THEN b.balnce * x.sgn ELSE 0 END) AS period_actual,
       SUM(CASE WHEN b.postyr = @Yr   AND b.actprd <= @Prd THEN b.balnce * x.sgn ELSE 0 END) AS ytd_actual,
       SUM(CASE WHEN b.postyr = @PyYr AND b.actprd  = @Prd THEN b.balnce * x.sgn ELSE 0 END) AS prior_year_period,
       SUM(CASE WHEN b.postyr = @PyYr AND b.actprd <= @Prd THEN b.balnce * x.sgn ELSE 0 END) AS prior_year_ytd,
       SUM(CASE WHEN b.postyr = @Yr   AND b.actprd  = @Prd THEN b.budget * x.sgn ELSE 0 END) AS period_budget,
       SUM(CASE WHEN b.postyr = @Yr   AND b.actprd <= @Prd THEN b.budget * x.sgn ELSE 0 END) AS ytd_budget
FROM act x
JOIN dbo.lgrbal b ON b.lgract = x.recnum AND b.postyr IN (@Yr, @PyYr) AND b.actprd <= @Prd
GROUP BY x.sec_ord, x.section, x.grp_acct, x.grp_name
HAVING SUM(ABS(b.balnce)) <> 0
ORDER BY x.sec_ord, x.grp_acct;

/* ---------------------------------------------------------------------------
   2b. INCOME STATEMENT - SUMMARY LINES  (also drives the Prior Year Comparison tab)
   --------------------------------------------------------------------------- */
;WITH m AS (
    SELECT b.postyr, b.actprd, a.acttyp, b.balnce
    FROM dbo.lgrbal b JOIN dbo.lgract a ON a.recnum = b.lgract
    WHERE a.acttyp BETWEEN 11 AND 17 AND b.postyr IN (@Yr, @PyYr) AND b.actprd <= @Prd
), s AS (
    SELECT postyr,
      SUM(CASE WHEN actprd=@Prd AND acttyp=11 THEN balnce END) AS p_rev,
      SUM(CASE WHEN actprd=@Prd AND acttyp IN (13,14) THEN balnce END) AS p_dir,
      SUM(CASE WHEN actprd=@Prd AND acttyp IN (15,16) THEN balnce END) AS p_ind,
      SUM(CASE WHEN actprd=@Prd AND acttyp=12 THEN balnce END) AS p_oth,
      SUM(CASE WHEN actprd=@Prd AND acttyp=17 THEN balnce END) AS p_tax,
      SUM(CASE WHEN acttyp=11 THEN balnce END) AS y_rev,
      SUM(CASE WHEN acttyp IN (13,14) THEN balnce END) AS y_dir,
      SUM(CASE WHEN acttyp IN (15,16) THEN balnce END) AS y_ind,
      SUM(CASE WHEN acttyp=12 THEN balnce END) AS y_oth,
      SUM(CASE WHEN acttyp=17 THEN balnce END) AS y_tax
    FROM m GROUP BY postyr
)
SELECT '2b-IncomeStatement summary' AS report, postyr,
       p_rev AS period_total_operating_income, p_dir AS period_total_direct_expense,
       p_rev - p_dir AS period_gross_profit,
       (p_rev - p_dir) / NULLIF(p_rev,0) AS period_gross_profit_pct,
       p_ind AS period_total_indirect_expense, p_rev - p_dir - p_ind AS period_income_from_operations,
       p_rev - p_dir - p_ind + ISNULL(p_oth,0) - ISNULL(p_tax,0) AS period_net_income,
       y_rev AS ytd_total_operating_income, y_dir AS ytd_total_direct_expense,
       y_rev - y_dir AS ytd_gross_profit,
       (y_rev - y_dir) / NULLIF(y_rev,0) AS ytd_gross_profit_pct,
       y_ind AS ytd_total_indirect_expense, y_rev - y_dir - y_ind AS ytd_income_from_operations,
       (y_rev - y_dir - y_ind) / NULLIF(y_rev,0) AS ytd_income_from_operations_pct,
       y_rev - y_dir - y_ind + ISNULL(y_oth,0) - ISNULL(y_tax,0) AS ytd_net_income
FROM s ORDER BY postyr DESC;

/* ---------------------------------------------------------------------------
   3. OVER / UNDER BILLINGS  (per job, as of @AsOf)
      Cost      = posted job cost (jobcst.status = 1) with trndte <= @AsOf                      [ties to workbook]
      Contract  = original contract + change orders approved on/before @AsOf
                  (actrec.cntrct + prmchg.appamt, status 1)                                       [ties to workbook]
      Billed    = invoices status 1 (open) or 4 (paid) dated <= @AsOf  (status 5 = not posted)   [ties to workbook]
      Budget    = estimated total cost of the job.  The workbook keeps this by hand, so it is taken from
                  @JobBudget below (carried forward from the July workbook's Budget column).  For any job not listed there the
                  system budget (bdglin.ttlbdg + approved change-order cost) is used.
                  ==> UPDATE @JobBudget EACH MONTH (job_no, budget); jobs that appear in the second
                      result set below but are not in the list need a decision.
      % Comp    = Cost / Budget            Earned = Contract x % Comp
      Over/Under= Earned - Billed   (negative = overbilled -> liability 21000; positive = underbilled -> asset 11900)
   --------------------------------------------------------------------------- */
DECLARE @JobBudget TABLE (job_no bigint PRIMARY KEY, budget decimal(18,2) NOT NULL);

SELECT job_no, jobnme, cost, budget, budget_source, contract, billed INTO #ou_base FROM (
    SELECT r.recnum AS job_no, r.jobnme,
           ISNULL((SELECT SUM(cstamt) FROM dbo.jobcst c WHERE c.jobnum = r.recnum AND c.status = 1 AND c.trndte <= @AsOf), 0) AS cost,
           COALESCE(jb.budget,
                    ISNULL((SELECT SUM(ttlbdg) FROM dbo.bdglin b WHERE b.recnum = r.recnum), 0)
                  + ISNULL((SELECT SUM(cstamt) FROM dbo.prmchg p WHERE p.jobnum = r.recnum AND p.status = 1 AND p.aprdte <= @AsOf), 0)) AS budget,
           CASE WHEN jb.job_no IS NOT NULL THEN 'workbook list' ELSE 'system (bdglin+CO)' END AS budget_source,
           r.cntrct + ISNULL((SELECT SUM(appamt) FROM dbo.prmchg p WHERE p.jobnum = r.recnum AND p.status = 1 AND p.aprdte <= @AsOf), 0) AS contract,
           ISNULL((SELECT SUM(invttl) FROM dbo.acrinv i WHERE i.jobnum = r.recnum AND i.status IN (1,4) AND i.invdte <= @AsOf), 0) AS billed
    FROM dbo.actrec r
    LEFT JOIN @JobBudget jb ON jb.job_no = r.recnum
    WHERE r.status IN (4,5)
) x;

SELECT '3-OverUnder' AS report, job_no, jobnme, cost, budget,
       CAST(cost / NULLIF(budget,0) * 100 AS decimal(9,2)) AS pct_complete,
       contract,
       CAST(contract * cost / NULLIF(budget,0) AS decimal(18,2)) AS earned,
       billed,
       CAST(contract * cost / NULLIF(budget,0) - billed AS decimal(18,2)) AS over_under,
       budget_source
FROM #ou_base
WHERE (cost <> 0 OR billed <> 0)
ORDER BY job_no;

/* Totals vs. GL  (over = 21000 Overbillings, under = 11900 Underbillings) */
SELECT '3-OverUnder totals vs GL' AS report,
       SUM(CASE WHEN ou < 0 THEN -ou ELSE 0 END) AS overbillings_report,
       (SELECT balnce FROM dbo.lgrbal WHERE lgract = 21000 AND postyr = @Yr AND actprd = @Prd) AS overbillings_gl_21000,
       SUM(CASE WHEN ou > 0 THEN  ou ELSE 0 END) AS underbillings_report,
       (SELECT balnce FROM dbo.lgrbal WHERE lgract = 11900 AND postyr = @Yr AND actprd = @Prd) AS underbillings_gl_11900
FROM (SELECT contract * cost / NULLIF(budget,0) - billed AS ou FROM #ou_base
      WHERE (cost <> 0 OR billed <> 0)) t;

/* ---------------------------------------------------------------------------
   4. AR AGING  (invoice level, as of @AsOf)
      Balance   = invoice total - receipts (amount + discount) dated <= @AsOf            [ties to workbook for most jobs]
      Retained  = retainage held: acrinv.retain when populated, otherwise invoice total x the job's retainage %
                  (actrec.retain, else jobphs.retain); never more than the balance, never negative.
      Total due = Balance - Retained, aged by days past DUE DATE:
                  Current (not yet due) | 1-30 | 31-60 | 61-90 | 91+
      NOTE: the workbook's Retained and aging columns contain manual overrides on some jobs
            (e.g. 10141, 10142, 10152, 10173, 10182); expect differences on those.
   --------------------------------------------------------------------------- */
SELECT i.jobnum AS job_no, i.invnum AS invoice_no, i.dscrpt, i.duedte AS due_date,
       i.invttl - ISNULL(p.paid, 0) AS balance,
       CAST(0 AS decimal(18,2)) AS retained,
       DATEDIFF(day, i.duedte, @AsOf) AS days_past_due,
       COALESCE(NULLIF(r.retain, 0), (SELECT MAX(ph.retain) FROM dbo.jobphs ph WHERE ph.recnum = r.recnum), 0) AS job_ret_pct,
       i.retain AS inv_retain, i.invttl
INTO #ar
FROM dbo.acrinv i
JOIN dbo.actrec r ON r.recnum = i.jobnum
LEFT JOIN (SELECT _idref, SUM(amount + dsctkn) AS paid FROM dbo.acrpmt WHERE chkdte <= @AsOf GROUP BY _idref) p
       ON p._idref = i._idnum
WHERE i.status IN (1,4) AND i.invdte <= @AsOf;

DELETE FROM #ar WHERE ABS(balance) <= 0.004;
UPDATE #ar SET retained =
    CASE WHEN balance <= 0 THEN 0
         WHEN inv_retain > 0 THEN CASE WHEN inv_retain > balance THEN balance ELSE inv_retain END
         WHEN invttl * job_ret_pct / 100.0 > balance THEN balance
         ELSE invttl * job_ret_pct / 100.0 END;

SELECT '4-ARAging' AS report, a.job_no, r.jobnme, a.invoice_no, a.dscrpt, a.due_date, a.balance, a.retained,
       CASE WHEN days_past_due <= 0 THEN balance - retained ELSE 0 END AS [current],
       CASE WHEN days_past_due BETWEEN 1  AND 30 THEN balance - retained ELSE 0 END AS d1_30,
       CASE WHEN days_past_due BETWEEN 31 AND 60 THEN balance - retained ELSE 0 END AS d31_60,
       CASE WHEN days_past_due BETWEEN 61 AND 90 THEN balance - retained ELSE 0 END AS d61_90,
       CASE WHEN days_past_due > 90 THEN balance - retained ELSE 0 END AS d91_plus,
       balance - retained AS total_due
FROM #ar a JOIN dbo.actrec r ON r.recnum = a.job_no
ORDER BY a.job_no, a.due_date, a.invoice_no;

/* AR aging - job totals + grand total */
SELECT '4-ARAging totals' AS report, ISNULL(CAST(job_no AS varchar(10)), 'GRAND TOTAL') AS job_no,
       SUM(balance) AS balance, SUM(retained) AS retained,
       SUM(CASE WHEN days_past_due <= 0 THEN balance - retained ELSE 0 END) AS [current],
       SUM(CASE WHEN days_past_due BETWEEN 1  AND 30 THEN balance - retained ELSE 0 END) AS d1_30,
       SUM(CASE WHEN days_past_due BETWEEN 31 AND 60 THEN balance - retained ELSE 0 END) AS d31_60,
       SUM(CASE WHEN days_past_due BETWEEN 61 AND 90 THEN balance - retained ELSE 0 END) AS d61_90,
       SUM(CASE WHEN days_past_due > 90 THEN balance - retained ELSE 0 END) AS d91_plus,
       SUM(balance - retained) AS total_due
FROM #ar
GROUP BY ROLLUP (job_no)
ORDER BY GROUPING(job_no), job_no;

/* ---------------------------------------------------------------------------
   5. FINANCIAL STATEMENTS  (workbook tabs "FS Balance Sheet 2026" and "FS Stmt of Inc & RE")
   5a. FS Balance Sheet - same ledger, reclassified for presentation:
       - Contract Assets*      = Underbillings (11900) less the contract offset
       - Contract Liabilities* = -(contract offset); Overbillings (21000) stays inside Accrued Liabilities
       - Contract offset       = per job, the smaller of the overbilling and the retainage held on AR
                                 (workbook Over/Under columns P and Q); assets and liabilities are both
                                 reduced by it, so total assets = ledger assets - offset.
       - Software is shown net of its amortization under Other Assets; PP&E net of its own depreciation.
   --------------------------------------------------------------------------- */
DECLARE @Offset decimal(18,2) = ISNULL((
    SELECT SUM(CASE WHEN o.ou < 0 THEN CASE WHEN -o.ou < ISNULL(r.retained,0) THEN -o.ou ELSE ISNULL(r.retained,0) END ELSE 0 END)
    FROM (SELECT job_no, contract * cost / NULLIF(budget,0) - billed AS ou FROM #ou_base
          WHERE (cost <> 0 OR billed <> 0)) o
    LEFT JOIN (SELECT job_no, SUM(retained) AS retained FROM #ar GROUP BY job_no) r ON r.job_no = o.job_no), 0);

;WITH b AS (
    SELECT a.recnum, a.acttyp, b.balnce
    FROM dbo.lgract a JOIN dbo.lgrbal b ON b.lgract = a.recnum AND b.postyr = @Yr AND b.actprd = @Prd
    WHERE a.acttyp BETWEEN 1 AND 10 AND a.recnum <> 33000
), ni AS (
    SELECT b.postyr, SUM(CASE WHEN a.acttyp IN (11,12) THEN b.balnce ELSE -b.balnce END) AS net_income
    FROM dbo.lgrbal b JOIN dbo.lgract a ON a.recnum = b.lgract
    WHERE a.acttyp BETWEEN 11 AND 17 AND (b.postyr < @Yr OR (b.postyr = @Yr AND b.actprd <= @Prd))
    GROUP BY b.postyr
), l AS (
    SELECT 10 AS ord, 'Cash and Cash Equivalents' AS line, SUM(CASE WHEN acttyp = 1 THEN balnce END) AS amount FROM b
    UNION ALL SELECT 20, 'Contract Receivables',  SUM(CASE WHEN recnum = 11200 THEN balnce END) FROM b
    UNION ALL SELECT 30, 'Other Receivables',     SUM(CASE WHEN acttyp IN (2,3) AND recnum NOT IN (11200,11900) THEN balnce END) FROM b
    UNION ALL SELECT 40, 'Prepaid Expenses and Other Current Assets', SUM(CASE WHEN acttyp = 4 THEN balnce END) FROM b
    UNION ALL SELECT 50, 'Contract Assets*',      SUM(CASE WHEN recnum = 11900 THEN balnce END) - @Offset FROM b
    UNION ALL SELECT 60, 'Vehicles',              SUM(CASE WHEN recnum = 18100 THEN balnce END) FROM b
    UNION ALL SELECT 61, 'Office Equipment',      SUM(CASE WHEN recnum = 18400 THEN balnce END) FROM b
    UNION ALL SELECT 62, 'Furniture and Fixtures',SUM(CASE WHEN recnum = 18600 THEN balnce END) FROM b
    UNION ALL SELECT 63, 'Less Accumulated Depreciation', -SUM(CASE WHEN acttyp = 6 AND recnum <> 19450 THEN balnce END) FROM b
    UNION ALL SELECT 70, 'Software Costs, net of Accumulated Amortization',
                         SUM(CASE WHEN recnum = 18450 THEN balnce WHEN recnum = 19450 THEN -balnce END) FROM b
    UNION ALL SELECT 110, 'Accounts Payable',      SUM(CASE WHEN recnum = 20000 THEN balnce END) FROM b
    UNION ALL SELECT 120, 'Accrued Liabilities',   SUM(CASE WHEN acttyp = 7 AND recnum NOT IN (20000,20650) THEN balnce END) FROM b
    UNION ALL SELECT 130, 'Notes Payable',         0
    UNION ALL SELECT 140, 'Distributions Payable', SUM(CASE WHEN recnum = 20650 THEN balnce END) FROM b
    UNION ALL SELECT 150, 'Contract Liabilities*', -@Offset
    UNION ALL SELECT 160, 'Notes Payable to Stockholders', SUM(CASE WHEN acttyp = 8 THEN balnce END) FROM b
    UNION ALL SELECT 170, 'Common Stock',          SUM(CASE WHEN acttyp = 9 THEN balnce END) FROM b
    UNION ALL SELECT 180, 'Retained Earnings',
           (SELECT balnce FROM dbo.lgrbal WHERE lgract = 33000 AND postyr = @Yr AND actprd = @Prd)
         + (SELECT ISNULL(SUM(net_income),0) FROM ni WHERE postyr BETWEEN @ReFirstYr AND @Yr - 1)
         + (SELECT ISNULL(SUM(net_income),0) FROM ni WHERE postyr = @Yr)
         - (SELECT SUM(CASE WHEN recnum = 35000 THEN balnce END) FROM b)
)
SELECT '5a-FS Balance Sheet' AS report, ord, line, ISNULL(amount,0) AS amount FROM l
UNION ALL SELECT '5a-FS Balance Sheet', 55, 'Total Current Assets',            SUM(amount) FROM l WHERE ord BETWEEN 10 AND 50
UNION ALL SELECT '5a-FS Balance Sheet', 64, 'Total Property and Equipment',    SUM(amount) FROM l WHERE ord BETWEEN 60 AND 63
UNION ALL SELECT '5a-FS Balance Sheet', 80, 'Total Assets',                    SUM(amount) FROM l WHERE ord BETWEEN 10 AND 70
UNION ALL SELECT '5a-FS Balance Sheet', 155,'Total Current Liabilities',       SUM(amount) FROM l WHERE ord BETWEEN 110 AND 150
UNION ALL SELECT '5a-FS Balance Sheet', 165,'Total Liabilities',               SUM(amount) FROM l WHERE ord BETWEEN 110 AND 160
UNION ALL SELECT '5a-FS Balance Sheet', 190,'Total Stockholder''s Equity',     SUM(amount) FROM l WHERE ord IN (170,180)
UNION ALL SELECT '5a-FS Balance Sheet', 200,'Total Liabilities and Equity',    SUM(amount) FROM l WHERE ord BETWEEN 110 AND 180
UNION ALL SELECT '5a-FS Balance Sheet', 210,'Out of balance (must be 0.00)',
       (SELECT SUM(amount) FROM l WHERE ord BETWEEN 10 AND 70) - (SELECT SUM(amount) FROM l WHERE ord BETWEEN 110 AND 180)
ORDER BY ord;

/* 5b. FS Statement of Income and Retained Earnings - one row per year
       (current year = year to date through @Prd; earlier years = full year).
       Contract Revenues exclude Discounts Earned (44000) and interest income (42000), which the FS shows as Miscellaneous Income;
       interest expense is taken out of Operating Expenses and shown under Other Income (Expense).
       Retained earnings at end = beginning + net income - distributions.
       Beginning RE = 33000 for the year + net income of every year from @ReFirstYr up to the prior year. */
;WITH m AS (
    SELECT b.postyr, a.recnum, a.acttyp, b.balnce
    FROM dbo.lgrbal b JOIN dbo.lgract a ON a.recnum = b.lgract
    WHERE a.acttyp BETWEEN 11 AND 17 AND (b.postyr < @Yr OR (b.postyr = @Yr AND b.actprd <= @Prd))
), y AS (
    SELECT postyr,
      SUM(CASE WHEN acttyp = 11 AND recnum NOT IN (42000,44000) THEN balnce END)                            AS revenue,
      SUM(CASE WHEN acttyp IN (13,14) THEN balnce END)                                              AS cost_of_rev,
      SUM(CASE WHEN acttyp IN (15,16) AND recnum NOT IN (61200,61210) THEN balnce END)             AS opex,
      SUM(CASE WHEN recnum IN (42000,47000) THEN balnce END)                                       AS interest_income,
      ISNULL(SUM(CASE WHEN (acttyp = 12 AND recnum NOT IN (42000,47000)) OR recnum = 44000 THEN balnce END),0)
        - ISNULL(SUM(CASE WHEN acttyp = 17 THEN balnce END),0)                                     AS misc_income,
      SUM(CASE WHEN recnum IN (61200,61210) THEN balnce END)                                       AS interest_expense
    FROM m GROUP BY postyr
), s AS (
    SELECT y.*,
           ISNULL(revenue,0) - ISNULL(cost_of_rev,0) AS gross_profit,
           ISNULL(revenue,0) - ISNULL(cost_of_rev,0) - ISNULL(opex,0) AS income_before_other,
           ISNULL(interest_income,0) + misc_income - ISNULL(interest_expense,0) AS total_other,
           ISNULL(revenue,0) - ISNULL(cost_of_rev,0) - ISNULL(opex,0)
             + ISNULL(interest_income,0) + misc_income - ISNULL(interest_expense,0) AS net_income
    FROM y
)
SELECT '5b-FS Income & RE' AS report, s.postyr AS [year],
       s.revenue AS contract_revenues, s.cost_of_rev AS costs_of_contract_revenues, s.gross_profit,
       s.opex AS operating_expenses, s.income_before_other,
       s.interest_income, s.misc_income AS miscellaneous_income, -ISNULL(s.interest_expense,0) AS interest_expense,
       s.total_other AS total_other_income_expense, s.net_income,
       re.beg_re AS retained_earnings_beginning,
       d.dist AS less_distributions,
       re.beg_re + s.net_income - d.dist AS retained_earnings_end
FROM s
CROSS APPLY (SELECT ISNULL((SELECT balnce FROM dbo.lgrbal WHERE lgract = 33000 AND postyr = s.postyr AND actprd = 1), 0)
                  + ISNULL((SELECT SUM(s2.net_income) FROM s s2 WHERE s2.postyr BETWEEN @ReFirstYr AND s.postyr - 1), 0) AS beg_re) re
CROSS APPLY (SELECT ISNULL((SELECT balnce FROM dbo.lgrbal
                            WHERE lgract = 35000 AND postyr = s.postyr
                              AND actprd = CASE WHEN s.postyr = @Yr THEN @Prd ELSE 12 END), 0) AS dist) d
ORDER BY s.postyr DESC;

/* ---------------------------------------------------------------------------
   6. VALIDATION CHECKS  - compare the two numbers on each row
   --------------------------------------------------------------------------- */
SELECT '6-Check: AR sub-ledger vs GL 11200' AS check_name,
       (SELECT SUM(balance) FROM #ar) AS value_a,
       (SELECT balnce FROM dbo.lgrbal WHERE lgract = 11200 AND postyr = @Yr AND actprd = @Prd) AS value_b
UNION ALL
SELECT '6-Check: posted job cost (jobcst) vs GL direct expense, YTD',
       (SELECT SUM(cstamt) FROM dbo.jobcst WHERE status = 1 AND postyr = @Yr AND actprd <= @Prd),
       (SELECT SUM(b.balnce) FROM dbo.lgrbal b JOIN dbo.lgract a ON a.recnum = b.lgract
         WHERE a.acttyp IN (13,14) AND b.postyr = @Yr AND b.actprd <= @Prd)
UNION ALL
SELECT '6-Check: GL income-statement rows revised after period end (count)',
       (SELECT COUNT(*) FROM dbo.lgrbal WHERE postyr = @Yr AND actprd <= @Prd AND upddte > DATEADD(day, 1, @AsOf)),
       0;
DROP TABLE #ar;
DROP TABLE #ou_base;
