import { NextRequest, NextResponse } from "next/server";
import { toErrorResponse } from "@/lib/api-utils";
import { deleteMatch, getMatch, updateMatch } from "@/lib/services/match-service";

interface Params {
  params: { id: string };
}

export async function GET(_request: NextRequest, { params }: Params) {
  try {
    const match = await getMatch(params.id);
    return NextResponse.json(match);
  } catch (error) {
    return toErrorResponse(error);
  }
}

export async function PATCH(request: NextRequest, { params }: Params) {
  try {
    const body = await request.json();
    const match = await updateMatch(params.id, {
      playedAt: new Date(body.playedAt),
      playerAId: body.playerAId,
      playerBId: body.playerBId,
      playerASets: Number(body.playerASets),
      playerBSets: Number(body.playerBSets),
      notes: body.notes,
    });
    return NextResponse.json(match);
  } catch (error) {
    return toErrorResponse(error);
  }
}

export async function DELETE(_request: NextRequest, { params }: Params) {
  try {
    await deleteMatch(params.id);
    return NextResponse.json({ success: true });
  } catch (error) {
    return toErrorResponse(error);
  }
}
