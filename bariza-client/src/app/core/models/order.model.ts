export type OrderStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Processing'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled'
  | 'Rejected';

export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  imageUrl?: string | null;
  unitPrice: number;
  quantity: number;
  subtotal: number;
  sellerName?: string | null;
  isAvailable: boolean;
}

export interface Cart {
  id: number;
  items: CartItem[];
  totalAmount: number;
}

export interface AddCartItemRequest {
  productId: number;
  quantity: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
}

export interface ShippingAddressRequest {
  fullName: string;
  phone: string;
  country: string;
  city: string;
  address: string;
}

export interface ShippingAddress {
  id: number;
  fullName: string;
  phone: string;
  country: string;
  city: string;
  address: string;
}

export interface OrderItem {
  id: number;
  productId?: number | null;
  productName: string;
  imageUrl?: string | null;
  unitPrice: number;
  quantity: number;
  subtotal: number;
  sellerName?: string | null;
}

export interface SellerOrder {
  id: number;
  orderId: number;
  orderNumber: string;
  sellerStoreName: string;
  status: OrderStatus;
  totalAmount: number;
  createdUtc: string;
  customerName: string;
  shippingAddress: ShippingAddress;
  items: OrderItem[];
}

export interface Order {
  id: number;
  orderNumber: string;
  checkoutReference: string;
  status: OrderStatus;
  totalAmount: number;
  createdUtc: string;
  shippingAddress: ShippingAddress;
  sellerOrders: SellerOrder[];
}

export interface UpdateOrderStatusRequest {
  status: OrderStatus;
}
