ALTER TABLE food_emolite.store_notifications
    ADD COLUMN IF NOT EXISTS store_food_id BIGINT;

ALTER TABLE food_emolite.store_notifications
    ADD COLUMN IF NOT EXISTS food_name VARCHAR(255);

ALTER TABLE food_emolite.store_notifications
    ADD COLUMN IF NOT EXISTS quantity INTEGER;
