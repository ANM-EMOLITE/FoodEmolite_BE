ALTER TABLE food_emolite.order_items
    ADD COLUMN IF NOT EXISTS cost_price NUMERIC(18, 2) NOT NULL DEFAULT 0;

-- Tuỳ chọn: đơn cũ chưa có giá vốn lúc bán -> gán tạm bằng giá vốn hiện tại của món.
-- Chỉ chạy SAU khi đã nhập giá vốn cho các món, và chỉ chạy 1 lần.
-- UPDATE food_emolite.order_items oi
-- SET cost_price = f.cost_price
-- FROM food_emolite.store_foods f
-- WHERE f.id = oi.store_food_id
--   AND oi.cost_price = 0;
