-- Chạy 1 lần DUY NHẤT, ngay trước khi deploy bản có hoàn kho khi huỷ đơn.
-- Cộng bù số lượng cho các đơn đã huỷ trước đó (lúc huỷ chưa được hoàn kho).
-- Bỏ qua nếu đã sửa tay số lượng / đã kiểm kho sau khi các đơn này bị huỷ — sẽ bị cộng trùng.

-- Xem trước số lượng sẽ cộng bù:
-- SELECT f.id, f.food_name, f.quantity, x.returned, f.quantity + x.returned AS new_quantity
-- FROM food_emolite.store_foods f
-- JOIN (
--     SELECT oi.store_food_id, SUM(oi.quantity) AS returned
--     FROM food_emolite.order_items oi
--     JOIN food_emolite.orders o ON o.id = oi.order_id
--     WHERE o.order_status = 'CANCELLED'
--     GROUP BY oi.store_food_id
-- ) x ON x.store_food_id = f.id;

BEGIN;

UPDATE food_emolite.store_foods f
SET quantity   = f.quantity + x.returned,
    updated_at = NOW()
FROM (
    SELECT oi.store_food_id, SUM(oi.quantity) AS returned
    FROM food_emolite.order_items oi
    JOIN food_emolite.orders o ON o.id = oi.order_id
    WHERE o.order_status = 'CANCELLED'
    GROUP BY oi.store_food_id
) x
WHERE x.store_food_id = f.id;

COMMIT;
