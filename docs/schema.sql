-- Schema CRM/CMS bán phụ tùng ô tô BanHangcoi
-- Database mục tiêu: PostgreSQL 14+
-- Mục đích: chuyển dữ liệu lead, đơn hàng, vận đơn, kho từ Excel vào hệ thống live có truy vết.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TYPE user_role AS ENUM (
  'admin',
  'manager',
  'sale',
  'warehouse',
  'shipping',
  'support',
  'marketing',
  'finance',
  'qa_readonly'
);

CREATE TYPE lead_status AS ENUM (
  'new',
  'assigned',
  'called_once',
  'follow_up',
  'no_answer',
  'low_intent',
  'high_intent',
  'qualified',
  'closed_won',
  'closed_lost',
  'recycled',
  'duplicate',
  'invalid'
);

CREATE TYPE order_status AS ENUM (
  'draft',
  'confirmed',
  'waiting_pack',
  'packed',
  'handover_to_carrier',
  'shipping',
  'delivery_issue',
  'delivered',
  'returned',
  'cancelled',
  'done'
);

CREATE TYPE ticket_status AS ENUM (
  'open',
  'in_progress',
  'waiting_customer',
  'resolved',
  'closed'
);

CREATE TYPE inventory_movement_type AS ENUM (
  'opening',
  'purchase_in',
  'sale_out',
  'return_in',
  'defect_out',
  'adjustment'
);

CREATE TABLE import_batches (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  batch_name text NOT NULL,
  source_file text NOT NULL,
  imported_by uuid,
  imported_at timestamptz NOT NULL DEFAULT now(),
  status text NOT NULL DEFAULT 'created',
  raw_rows integer NOT NULL DEFAULT 0,
  staged_rows integer NOT NULL DEFAULT 0,
  accepted_rows integer NOT NULL DEFAULT 0,
  rejected_rows integer NOT NULL DEFAULT 0,
  notes text
);

CREATE TABLE stg_excel_rows (
  id bigserial PRIMARY KEY,
  import_batch_id uuid NOT NULL REFERENCES import_batches(id),
  source_file text NOT NULL,
  source_sheet text NOT NULL,
  source_row integer NOT NULL,
  row_json jsonb NOT NULL,
  row_hash text NOT NULL,
  imported_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (import_batch_id, source_sheet, source_row)
);

CREATE TABLE stg_rejections (
  id bigserial PRIMARY KEY,
  import_batch_id uuid NOT NULL REFERENCES import_batches(id),
  source_file text NOT NULL,
  source_sheet text NOT NULL,
  source_row integer NOT NULL,
  entity_type text NOT NULL,
  field_name text,
  raw_value text,
  rejection_code text NOT NULL,
  rejection_message text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE app_users (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  display_name text NOT NULL,
  email text UNIQUE,
  phone text,
  role user_role NOT NULL,
  team_name text,
  is_active boolean NOT NULL DEFAULT true,
  password_hash text,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE customers (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  phone_normalized text NOT NULL,
  phone_raw text,
  customer_name text,
  address_raw text,
  province text,
  district text,
  ward text,
  first_source_file text,
  first_source_sheet text,
  first_source_row integer,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (phone_normalized)
);

CREATE TABLE customer_phones (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_id uuid NOT NULL REFERENCES customers(id),
  phone_normalized text NOT NULL,
  phone_raw text,
  is_primary boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (customer_id, phone_normalized)
);

CREATE TABLE products (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  sku text UNIQUE,
  product_name text NOT NULL,
  normalized_name text NOT NULL,
  category text,
  voltage text,
  color text,
  is_active boolean NOT NULL DEFAULT true,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE product_aliases (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  product_id uuid NOT NULL REFERENCES products(id),
  alias_raw text NOT NULL,
  alias_normalized text NOT NULL,
  source_note text,
  UNIQUE (alias_normalized)
);

CREATE TABLE marketing_campaigns (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  campaign_name text NOT NULL,
  page_name text,
  content_name text,
  product_focus text,
  is_active boolean NOT NULL DEFAULT true,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE marketing_spend_daily (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  campaign_id uuid REFERENCES marketing_campaigns(id),
  spend_date date NOT NULL,
  page_name text,
  content_name text,
  spend_vnd bigint NOT NULL DEFAULT 0,
  source_file text,
  source_sheet text,
  source_row integer,
  import_batch_id uuid REFERENCES import_batches(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (spend_date, page_name, content_name, import_batch_id)
);

CREATE TABLE crm_leads (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_id uuid REFERENCES customers(id),
  assigned_user_id uuid REFERENCES app_users(id),
  campaign_id uuid REFERENCES marketing_campaigns(id),
  lead_status lead_status NOT NULL DEFAULT 'new',
  lead_date date,
  phone_normalized text,
  customer_name_raw text,
  page_name text,
  content_name text,
  lead_type text,
  occupation text,
  vehicle_type text,
  vehicle_air_type text,
  product_interest_raw text,
  product_id uuid REFERENCES products(id),
  sale_name_raw text,
  next_action_at timestamptz,
  next_action_note text,
  source_file text NOT NULL,
  source_sheet text NOT NULL,
  source_row integer NOT NULL,
  import_batch_id uuid REFERENCES import_batches(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_crm_leads_phone ON crm_leads(phone_normalized);
CREATE INDEX idx_crm_leads_status_owner ON crm_leads(lead_status, assigned_user_id);
CREATE INDEX idx_crm_leads_source ON crm_leads(source_file, source_sheet, source_row);

CREATE TABLE crm_call_attempts (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  lead_id uuid NOT NULL REFERENCES crm_leads(id),
  attempt_no integer NOT NULL CHECK (attempt_no BETWEEN 1 AND 10),
  call_date date,
  call_started_at timestamptz,
  duration_seconds integer,
  result_raw text,
  result_status lead_status,
  pain_note text,
  next_follow_up_at timestamptz,
  created_by uuid REFERENCES app_users(id),
  source_file text,
  source_sheet text,
  source_row integer,
  import_batch_id uuid REFERENCES import_batches(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (lead_id, attempt_no, source_file, source_sheet, source_row)
);

CREATE TABLE orders (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  order_code text UNIQUE,
  lead_id uuid REFERENCES crm_leads(id),
  customer_id uuid REFERENCES customers(id),
  sale_user_id uuid REFERENCES app_users(id),
  order_status order_status NOT NULL DEFAULT 'draft',
  order_date date,
  confirmed_at timestamptz,
  packed_at timestamptz,
  delivered_at timestamptz,
  done_at timestamptz,
  customer_name_raw text,
  phone_normalized text,
  address_raw text,
  combo_raw text,
  cod_amount_vnd bigint,
  shipping_fee_vnd bigint,
  discount_vnd bigint NOT NULL DEFAULT 0,
  total_amount_vnd bigint,
  note_raw text,
  source_file text NOT NULL,
  source_sheet text NOT NULL,
  source_row integer NOT NULL,
  import_batch_id uuid REFERENCES import_batches(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CHECK (cod_amount_vnd IS NULL OR cod_amount_vnd >= 0),
  CHECK (shipping_fee_vnd IS NULL OR shipping_fee_vnd >= 0)
);

CREATE INDEX idx_orders_phone ON orders(phone_normalized);
CREATE INDEX idx_orders_status ON orders(order_status);
CREATE INDEX idx_orders_customer ON orders(customer_id);

CREATE TABLE order_items (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id uuid NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
  product_id uuid REFERENCES products(id),
  product_name_raw text NOT NULL,
  quantity integer NOT NULL DEFAULT 1 CHECK (quantity > 0),
  unit_price_vnd bigint,
  line_amount_vnd bigint,
  item_note text,
  source_file text,
  source_sheet text,
  source_row integer,
  import_batch_id uuid REFERENCES import_batches(id),
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE shipments (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id uuid NOT NULL REFERENCES orders(id),
  tracking_code text,
  carrier_name text,
  shipment_status order_status NOT NULL DEFAULT 'handover_to_carrier',
  shipped_at timestamptz,
  delivered_at timestamptz,
  return_reason text,
  delivery_issue_note text,
  source_file text,
  source_sheet text,
  source_row integer,
  import_batch_id uuid REFERENCES import_batches(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX uq_shipments_tracking_code
  ON shipments (tracking_code)
  WHERE tracking_code IS NOT NULL AND tracking_code <> '';

CREATE TABLE order_status_events (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id uuid NOT NULL REFERENCES orders(id),
  old_status order_status,
  new_status order_status NOT NULL,
  event_at timestamptz NOT NULL DEFAULT now(),
  actor_id uuid REFERENCES app_users(id),
  reason text,
  note text
);

CREATE TABLE inventory_movements (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  product_id uuid REFERENCES products(id),
  movement_type inventory_movement_type NOT NULL,
  movement_date date NOT NULL,
  quantity integer NOT NULL,
  order_id uuid REFERENCES orders(id),
  note text,
  source_file text,
  source_sheet text,
  source_row integer,
  import_batch_id uuid REFERENCES import_batches(id),
  created_by uuid REFERENCES app_users(id),
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE stock_snapshots (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  product_id uuid NOT NULL REFERENCES products(id),
  snapshot_date date NOT NULL,
  opening_qty integer NOT NULL DEFAULT 0,
  purchase_in_qty integer NOT NULL DEFAULT 0,
  sale_out_qty integer NOT NULL DEFAULT 0,
  return_in_qty integer NOT NULL DEFAULT 0,
  defect_qty integer NOT NULL DEFAULT 0,
  closing_qty integer NOT NULL DEFAULT 0,
  source_file text,
  source_sheet text,
  source_row integer,
  import_batch_id uuid REFERENCES import_batches(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (product_id, snapshot_date, import_batch_id)
);

CREATE TABLE support_tickets (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id uuid REFERENCES orders(id),
  customer_id uuid REFERENCES customers(id),
  assigned_user_id uuid REFERENCES app_users(id),
  status ticket_status NOT NULL DEFAULT 'open',
  issue_type text NOT NULL,
  issue_note text,
  resolution_note text,
  opened_at timestamptz NOT NULL DEFAULT now(),
  resolved_at timestamptz,
  closed_at timestamptz
);

CREATE TABLE kpi_targets (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid REFERENCES app_users(id),
  team_name text,
  target_month date NOT NULL,
  call_target integer,
  revenue_target_vnd bigint,
  order_target integer,
  product_category text,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (user_id, team_name, target_month, product_category)
);

CREATE TABLE audit_logs (
  id bigserial PRIMARY KEY,
  actor_id uuid REFERENCES app_users(id),
  actor_role user_role,
  entity_type text NOT NULL,
  entity_id uuid,
  action text NOT NULL,
  old_value jsonb,
  new_value jsonb,
  reason text,
  ip_address inet,
  device_info text,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX idx_audit_logs_actor ON audit_logs(actor_id, created_at);

CREATE VIEW vw_sales_funnel_daily AS
SELECT
  COALESCE(l.lead_date, o.order_date) AS report_date,
  l.page_name,
  l.content_name,
  u.display_name AS sale_name,
  count(DISTINCT l.id) AS leads,
  count(DISTINCT ca.id) AS call_attempts,
  count(DISTINCT l.id) FILTER (WHERE l.lead_status = 'high_intent') AS high_intent_leads,
  count(DISTINCT o.id) FILTER (WHERE o.order_status IN ('confirmed','waiting_pack','packed','handover_to_carrier','shipping','delivery_issue','delivered','done')) AS closed_orders,
  count(DISTINCT o.id) FILTER (WHERE o.order_status IN ('delivered','done')) AS delivered_orders,
  count(DISTINCT o.id) FILTER (WHERE o.order_status = 'returned') AS returned_orders,
  COALESCE(sum(o.total_amount_vnd) FILTER (WHERE o.order_status IN ('confirmed','waiting_pack','packed','handover_to_carrier','shipping','delivery_issue','delivered','done')), 0) AS closed_revenue_vnd,
  COALESCE(sum(o.total_amount_vnd) FILTER (WHERE o.order_status IN ('delivered','done')), 0) AS delivered_revenue_vnd
FROM crm_leads l
LEFT JOIN app_users u ON u.id = l.assigned_user_id
LEFT JOIN crm_call_attempts ca ON ca.lead_id = l.id
LEFT JOIN orders o ON o.lead_id = l.id
GROUP BY 1, 2, 3, 4;

CREATE VIEW vw_inventory_current AS
SELECT
  p.id AS product_id,
  p.sku,
  p.product_name,
  COALESCE(sum(
    CASE
      WHEN im.movement_type IN ('opening','purchase_in','return_in','adjustment') THEN im.quantity
      WHEN im.movement_type IN ('sale_out','defect_out') THEN -im.quantity
      ELSE 0
    END
  ), 0) AS current_qty
FROM products p
LEFT JOIN inventory_movements im ON im.product_id = p.id
GROUP BY p.id, p.sku, p.product_name;
