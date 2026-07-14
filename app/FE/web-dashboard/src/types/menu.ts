export interface CreateMenuItemRequest {
  name: string;
  description?: string;
  price: number;
  imageUrl?: string;
  categoryId: number;
}

// PATCH /menu-items/{id} là partial update ở BE — mọi field đều optional,
// chỉ field nào có trong request mới bị ghi đè.
export interface UpdateMenuItemRequest {
  name?: string;
  description?: string;
  price?: number;
  imageUrl?: string;
  categoryId?: number;
  isAvailable?: boolean;
}

// Response thật của PATCH /menu-items/{id} (MenuItemUpdatedResponse ở BE) chỉ trả về
// một tập con field, không phải toàn bộ MenuItemResponse.
export interface MenuItemPatchResponse {
  id: number;
  name: string;
  isAvailable: boolean;
  updatedAt: string;
}

export interface MenuItemResponse {
  id: number;
  name: string;
  description?: string;
  price: number;
  imageUrl?: string;
  categoryId: number;
  categoryName?: string;
  isAvailable: boolean;
  averageRating: number;
}

export interface MenuCategoryResponse {
  id: number;
  name: string;
  description?: string;
  itemCount: number;
}
