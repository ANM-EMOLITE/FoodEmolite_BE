ALTER TABLE food_emolite.activity_logs
    ADD COLUMN IF NOT EXISTS target_type VARCHAR(50);

ALTER TABLE food_emolite.activity_logs
    ADD COLUMN IF NOT EXISTS target_id BIGINT;

UPDATE food_emolite.activity_logs l SET target_type = 'ORDER', target_id = o.id
FROM food_emolite.orders o
WHERE l.target_id IS NULL
  AND l.action IN ('CREATE_ORDER', 'CONFIRM_PAYMENT', 'UPDATE_PAY_LATER_PRICE', 'CANCEL_ORDER')
  AND substring(l.description from '"([^"]+)"') = o.order_code;

UPDATE food_emolite.activity_logs l SET target_type = 'INVENTORY_RECEIPT', target_id = r.id
FROM food_emolite.inventory_receipts r
WHERE l.target_id IS NULL
  AND l.action = 'IMPORT_STOCK'
  AND substring(l.description from '"([^"]+)"') = r.receipt_code;

UPDATE food_emolite.activity_logs l SET target_type = 'INVENTORY_STOCKTAKE', target_id = s.id
FROM food_emolite.inventory_stocktakes s
WHERE l.target_id IS NULL
  AND l.action = 'STOCKTAKE'
  AND substring(l.description from '"([^"]+)"') = s.stocktake_code;
