-- Nguồn đơn + loại đơn + địa chỉ giao hàng cho bảng orders — entity: FoodEmolite.Domain.Entities.Order
-- Chạy SAU vn_administrative_units.sql. Chạy lại nhiều lần an toàn (IF NOT EXISTS).
--   order_source: POS (chủ cửa hàng tạo tại quầy) | WEB_USER (user đăng nhập đặt) | WEB_GUEST (khách vãng lai qua link)
--   order_type  : DINE_IN (tại quầy) | DELIVERY (giao hàng — bắt buộc có SĐT + địa chỉ)
-- Đơn cũ mặc định POS / DINE_IN.

ALTER TABLE food_emolite.orders
    ADD COLUMN IF NOT EXISTS order_source            VARCHAR(20)   NOT NULL DEFAULT 'POS',
    ADD COLUMN IF NOT EXISTS order_type              VARCHAR(20)   NOT NULL DEFAULT 'DINE_IN',
    ADD COLUMN IF NOT EXISTS delivery_phone          VARCHAR(20),
    ADD COLUMN IF NOT EXISTS delivery_province_code  VARCHAR(10),
    ADD COLUMN IF NOT EXISTS delivery_province_name  VARCHAR(255),
    ADD COLUMN IF NOT EXISTS delivery_ward_code      VARCHAR(10),
    ADD COLUMN IF NOT EXISTS delivery_ward_name      VARCHAR(255),
    ADD COLUMN IF NOT EXISTS delivery_street         VARCHAR(500),
    ADD COLUMN IF NOT EXISTS delivery_latitude       NUMERIC(9, 6),
    ADD COLUMN IF NOT EXISTS delivery_longitude      NUMERIC(9, 6);

-- Lọc danh sách đơn theo nguồn
CREATE INDEX IF NOT EXISTS ix_orders_store_order_source
    ON food_emolite.orders (store_ref_code, order_source);
