import { NextRequest, NextResponse } from "next/server";
import { toErrorResponse } from "@/lib/api-utils";
import { getPlayer, setPlayerActive, updatePlayer } from "@/lib/services/player-service";

interface Params {
  params: { id: string };
}

export async function GET(_request: NextRequest, { params }: Params) {
  try {
    const player = await getPlayer(params.id);
    return NextResponse.json(player);
  } catch (error) {
    return toErrorResponse(error);
  }
}

export async function PATCH(request: NextRequest, { params }: Params) {
  try {
    const body = await request.json();

    // A dedicated isActive-only payload is treated as archive/reactivate.
    if (Object.keys(body).length === 1 && typeof body.isActive === "boolean") {
      const player = await setPlayerActive(params.id, body.isActive);
      return NextResponse.json(player);
    }

    const player = await updatePlayer(params.id, body);
    return NextResponse.json(player);
  } catch (error) {
    return toErrorResponse(error);
  }
}
